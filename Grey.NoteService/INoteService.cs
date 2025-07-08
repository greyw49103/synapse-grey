
using Grey.Enums;
using Newtonsoft.Json.Linq;

namespace Grey.Interfaces
{
    public interface INoteService
    {
        AddOnType? GetAddOnType(string note);
        DeviceType GetDeviceType(string note);
        MaskType? GetMaskType(string note, DeviceType deviceType);
        string? GetOxygenTankLiters(string note, DeviceType deviceType);
        OxygenTankUseType? GetOxygenTankUseType(string note, DeviceType deviceType);
        string GetPhysicianNote(string fileName);
        string GetProviderName(string note);
        string GetQualifier(string note);
        Task<bool> SendDrExtract(string physicianNoteFile, string extractUrl);
        JObject CreateExtractJSON(string physicianNote);
        string GetLabelValue(string note, string label);
    }
}