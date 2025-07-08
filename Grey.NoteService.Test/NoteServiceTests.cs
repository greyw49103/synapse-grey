using System;
using System.IO;
using System.Threading.Tasks;
using Grey.Enums;
using Grey.Interfaces;
using Grey.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Grey.Test
{
    public class NoteServiceTests
    {
        private readonly Mock<ILogger<NoteService>> _loggerMock;
        private readonly NoteService _service;

        public NoteServiceTests()
        {
            _loggerMock = new Mock<ILogger<NoteService>>();
            _service = new NoteService(_loggerMock.Object);
        }

        [Fact]
        public void GetPhysicianNote_FileExists_ReturnsFileContent()
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "Test note content");
            var result = _service.GetPhysicianNote(tempFile);
            Assert.Equal("Test note content", result);
            File.Delete(tempFile);
        }

        [Fact]
        public void GetPhysicianNote_FileDoesNotExist_ReturnsDefaultNote()
        {
            var result = _service.GetPhysicianNote("nonexistent.txt");
            Assert.Equal(NoteService.DefaultNote, result);
        }

        [Theory]
        [InlineData("Patient needs a CPAP.", DeviceType.CPAP)]
        [InlineData("Patient needs an Oxygen Tank.", DeviceType.OxygenTank)]
        [InlineData("Patient needs a Wheelchair.", DeviceType.WheelChair)]
        [InlineData("Unknown device.", DeviceType.Unknown)]
        public void GetDeviceType_ReturnsExpectedType(string note, DeviceType expected)
        {
            var result = _service.GetDeviceType(note);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetMaskType_CpapWithFullFace_ReturnsFullFace()
        {
            var note = "Patient needs a CPAP with full face mask.";
            var result = _service.GetMaskType(note, DeviceType.CPAP);
            Assert.Equal(MaskType.FullFace, result);
        }

        [Fact]
        public void GetMaskType_NotCpapOrNoFullFace_ReturnsNull()
        {
            var note = "Patient needs a CPAP.";
            var result = _service.GetMaskType(note, DeviceType.CPAP);
            Assert.Null(result);

            result = _service.GetMaskType("full face mask", DeviceType.OxygenTank);
            Assert.Null(result);
        }

        [Fact]
        public void GetAddOnType_WithHumidifier_ReturnsHumidifier()
        {
            var note = "Patient needs a humidifier.";
            var result = _service.GetAddOnType(note);
            Assert.Equal(AddOnType.Humidifier, result);
        }

        [Fact]
        public void GetAddOnType_WithoutHumidifier_ReturnsNull()
        {
            var note = "No add-ons.";
            var result = _service.GetAddOnType(note);
            Assert.Null(result);
        }

        [Fact]
        public void GetQualifier_WithAhi_ReturnsQualifier()
        {
            var note = "AHI > 20";
            var result = _service.GetQualifier(note);
            Assert.Equal("AHI > 20", result);
        }

        [Fact]
        public void GetQualifier_WithoutAhi_ReturnsEmpty()
        {
            var note = "No qualifier.";
            var result = _service.GetQualifier(note);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetProviderName_WithDr_ReturnsProvider()
        {
            var note = "Ordered by Dr. Cameron.";
            var result = _service.GetProviderName(note);
            Assert.Equal("Dr. Cameron", result);
        }

        [Fact]
        public void GetProviderName_WithoutDr_ReturnsUnknown()
        {
            var note = "No provider.";
            var result = _service.GetProviderName(note);
            Assert.Equal("Unknown", result);
        }

        [Fact]
        public void GetLabelValue_LabelPresent_ReturnsValue()
        {
            var note = "Diagnosis: Asthma\nOther: Value";
            var result = _service.GetLabelValue(note, "Diagnosis:");
            Assert.Equal("Asthma", result);
        }

        [Fact]
        public void GetLabelValue_LabelAbsent_ReturnsUnknown()
        {
            var note = "No label here.";
            var result = _service.GetLabelValue(note, "Diagnosis:");
            Assert.Equal("Unknown", result);
        }

        [Fact]
        public void GetOxygenTankLiters_Valid_ReturnsLiters()
        {
            var note = "Oxygen Tank 2.5 L";
            var result = _service.GetOxygenTankLiters(note, DeviceType.OxygenTank);
            Assert.Equal("2.5 L", result);
        }

        [Fact]
        public void GetOxygenTankLiters_InvalidOrWrongDevice_ReturnsNull()
        {
            var note = "Oxygen Tank";
            var result = _service.GetOxygenTankLiters(note, DeviceType.OxygenTank);
            Assert.Null(result);

            result = _service.GetOxygenTankLiters("2.5 L", DeviceType.CPAP);
            Assert.Null(result);
        }

        [Theory]
        [InlineData("sleep and exertion", OxygenTankUseType.SleepAndExertion)]
        [InlineData("sleep", OxygenTankUseType.Sleep)]
        [InlineData("exertion", OxygenTankUseType.Exertion)]
        [InlineData("none", null)]
        public void GetOxygenTankUseType_VariousCases(string note, OxygenTankUseType? expected)
        {
            var result = _service.GetOxygenTankUseType(note, DeviceType.OxygenTank);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CreateExtractJSON_ValidNote_ProducesExpectedJson()
        {
            var note = "Patient Name: John Doe\nDOB: 01/01/1980\nDiagnosis: Asthma\nPatient needs a CPAP with full face mask and humidifier. AHI > 20. Ordered by Dr. Cameron.";
            var json = _service.CreateExtractJSON(note);

            Assert.Equal("CPAP", json["device"]);
            Assert.Equal("FullFace", json["mask_type"]);
            Assert.Equal("Humidifier", json["add_ons"]?[0]);
            Assert.Equal("AHI > 20", json["qualifier"]);
            Assert.Equal("Dr. Cameron", json["ordering_provider"]);
            Assert.Equal("John Doe", json["patient_name"]);
            Assert.Equal("01/01/1980", json["dob"]);
            Assert.Equal("Asthma", json["diagnosis"]);
        }

        [Fact]
        public async Task SendDrExtract_InvalidFile_ReturnsFalse()
        {
            var result = await _service.SendDrExtract("nonexistent.txt", "http://localhost");
            Assert.False(result);
        }
    }
}