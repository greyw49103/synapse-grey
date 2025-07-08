using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Grey.Enums;
using Grey.Interfaces;

namespace Grey.Services
{
    public class NoteService : INoteService
    {
        public const string DefaultNote = "Patient needs a CPAP with full face mask and humidifier. AHI > 20. Ordered by Dr. Cameron.";

        private readonly ILogger _logger;

        public NoteService(ILogger<NoteService> logger)
        {
            _logger = logger;
        }

        public string GetPhysicianNote(string fileName)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    return File.ReadAllText(fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error reading {fileName}: {ex.Message}");
                    return DefaultNote;
                }
            }
            return DefaultNote;
        }

        public DeviceType GetDeviceType(string note)
        {
            if (note.Contains("CPAP", StringComparison.OrdinalIgnoreCase))
            {
                return DeviceType.CPAP;
            }
            if (note.Contains("Oxygen Tank", StringComparison.OrdinalIgnoreCase))
            {
                return DeviceType.OxygenTank;
            }
            if (note.Contains("Wheelchair", StringComparison.OrdinalIgnoreCase))
            {
                return DeviceType.WheelChair;
            }
            return DeviceType.Unknown;
        }

        public MaskType? GetMaskType(string note, DeviceType deviceType)
        {
            if (deviceType == DeviceType.CPAP && note.Contains("full face", StringComparison.OrdinalIgnoreCase))
            {
                return MaskType.FullFace;
            }
            return null;
        }

        public AddOnType? GetAddOnType(string note)
        {
            if (note.Contains("humidifier", StringComparison.OrdinalIgnoreCase))
            {
                return AddOnType.Humidifier;
            }
            return null;
        }

        public string GetQualifier(string note)
        {
            if (note.Contains("AHI > 20", StringComparison.OrdinalIgnoreCase))
            {
                return "AHI > 20";
            }
            return string.Empty;
        }

        public string GetProviderName(string note)
        {
            int index = note.IndexOf("Dr.", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return note.Substring(index).Trim('.').ReplaceLineEndings("");
            }
            return "Unknown";
        }

        public string GetLabelValue(string note, string label)
        {
            int length = label.Length;
            int index = note.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            int startIndex = index + length;
            if (index >= 0)
            {
                int endIndex = note.IndexOf("\n", index, StringComparison.OrdinalIgnoreCase);
                return note.Substring(startIndex, endIndex - startIndex).Trim();
            }
            return "Unknown";
        }

        public string? GetOxygenTankLiters(string note, DeviceType deviceType)
        {
            if (deviceType == DeviceType.OxygenTank)
            {
                var match = Regex.Match(note, @"(\d+(\.\d+)?) ?L", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value + " L";
                }
            }

            return null;
        }

        public OxygenTankUseType? GetOxygenTankUseType(string note, DeviceType deviceType)
        {
            if (deviceType == DeviceType.OxygenTank)
            {
                if (note.Contains("sleep", StringComparison.OrdinalIgnoreCase) && note.Contains("exertion", StringComparison.OrdinalIgnoreCase))
                {
                    return OxygenTankUseType.SleepAndExertion;
                }
                if (note.Contains("sleep", StringComparison.OrdinalIgnoreCase))
                {
                    return OxygenTankUseType.Sleep;
                }
                if (note.Contains("exertion", StringComparison.OrdinalIgnoreCase))
                {
                    return OxygenTankUseType.Exertion;
                }
            }

            return null;
        }

        public async Task<bool> SendDrExtract(string physicianNoteFile, string extractUrl)
        {
            try
            {
                string physicianNote = GetPhysicianNote(physicianNoteFile);

                _logger.LogInformation($"Original file: \n{physicianNote}");

                var json = CreateExtractJSON(physicianNote);

                var stringJson = json.ToString();

                _logger.LogInformation($"JSON extract to send: \n{stringJson}"); 

                using (var httpClient = new HttpClient())
                {
                    var stringContent = new StringContent(stringJson, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(extractUrl, stringContent);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("SendDrExtract API call successful.");
                        return true;
                    }
                    else
                    {
                        _logger.LogError($"SendDrExtract API call failed: {response.ReasonPhrase}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error SendDrExtract: {ex.Message}");
                return false;
            }
        }

        public JObject CreateExtractJSON(string physicianNote)
        {
            try
            {
                DeviceType deviceType = GetDeviceType(physicianNote);
                MaskType? maskType = GetMaskType(physicianNote, deviceType);
                AddOnType? addOnType = GetAddOnType(physicianNote);
                string qualifier = GetQualifier(physicianNote);
                string providerName = GetProviderName(physicianNote);
                string? oxygenTankLiters = GetOxygenTankLiters(physicianNote, deviceType);
                OxygenTankUseType? oxygenTankUseType = GetOxygenTankUseType(physicianNote, deviceType);
                string diagnosis = GetLabelValue(physicianNote, "Diagnosis:");
                string patientName = GetLabelValue(physicianNote, "Patient Name:");
                string dob = GetLabelValue(physicianNote, "DOB:");

                var json = new JObject
                {
                    ["device"] = deviceType.ToString(),
                    ["liters"] = oxygenTankLiters,
                    ["usage"] = oxygenTankUseType?.ToString(),
                    ["diagnosis"] = diagnosis,
                    ["mask_type"] = maskType?.ToString(),
                    ["add_ons"] = addOnType != null ? new JArray(addOnType.ToString()!) : null,
                    ["qualifier"] = qualifier,
                    ["ordering_provider"] = providerName,
                    ["patient_name"] = patientName,
                    ["dob"] = dob,
                };

                foreach (var property in json.Properties().ToList())
                {
                    if (property.Value.Type == JTokenType.Null || string.IsNullOrEmpty(property.Value.ToString()))
                    {
                        json.Remove(property.Name);
                    }
                }

                return json;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating JSON extract: {ex.Message}");
                throw;
            }
        }


    }
}
