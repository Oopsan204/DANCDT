using System;
using DACDT_2026;

namespace DACDT_2026.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                CleansMastercamNcAndNormalizesLaserCommands();
                PreservesSupportedArcAndMotionCommands();
                CameraSelectionUsesFriendlyNameAndDetectsSwitch();
                CameraReconnectDelayIsOneSecond();
                Console.WriteLine("All tests passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static void CleansMastercamNcAndNormalizesLaserCommands()
        {
            string input = string.Join(Environment.NewLine, new[]
            {
                "%",
                "O0001 (MASTER CAM FILE)",
                "N10 G17 G21 G90 G54",
                "N20 T1 M6",
                "N30 G43 H1 Z15.",
                "N40 M8",
                "N50 G0 X10. Y20.",
                "N60 M3 S12000",
                "N70 G1 X30. Y20. F800.",
                "N80 M5",
                "N90 M9",
                "N100 G0 X0. Y0.",
                "N110 M30",
                "%"
            });

            NcGcodeCleaner.CleanResult result = NcGcodeCleaner.Clean(input);

            string expected = string.Join(Environment.NewLine, new[]
            {
                "G21 G90 G54",
                "G0 X10. Y20.",
                "M3",
                "G1 X30. Y20. F800.",
                "M4",
                "G0 X0. Y0.",
                "M30"
            });

            AssertEqual(expected, result.Text.Trim(), "Mastercam NC should be filtered to safe laser G-code.");
            AssertTrue(result.RemovedLineCount >= 5, "Cleaner should report removed unsupported/header lines.");
            AssertTrue(result.Warnings.Count > 0, "Cleaner should report warnings for removed or normalized lines.");
        }

        private static void PreservesSupportedArcAndMotionCommands()
        {
            string input = string.Join(Environment.NewLine, new[]
            {
                "G21",
                "G90",
                "G0 X0 Y0",
                "M3",
                "G2 X10 Y0 I5 J0 F600",
                "G3 X0 Y0 R5",
                "M4"
            });

            NcGcodeCleaner.CleanResult result = NcGcodeCleaner.Clean(input);

            AssertEqual(input, result.Text.Trim(), "Cleaner should preserve supported G0/G1/G2/G3 commands.");
            AssertEqual("0", result.RemovedLineCount.ToString(), "Supported file should not remove lines.");
        }

        private static void CameraSelectionUsesFriendlyNameAndDetectsSwitch()
        {
            var cameras = new[]
            {
                new CameraDeviceSelection.CameraDevice("Integrated Camera", "@device:pnp:camera0"),
                new CameraDeviceSelection.CameraDevice("USB Camera", "@device:pnp:usb#vid_1234")
            };

            CameraDeviceSelection.CameraDevice selected = CameraDeviceSelection.FindByMonikerOrPreferred(cameras, "@device:pnp:camera0");

            AssertEqual("Integrated Camera", selected.DisplayName, "Camera UI should display the friendly camera name.");
            AssertTrue(CameraDeviceSelection.ShouldSwitch("@device:pnp:camera0", "@device:pnp:usb#vid_1234"), "Selecting a different camera while running should trigger a switch.");
            AssertTrue(!CameraDeviceSelection.ShouldSwitch("@device:pnp:camera0", "@device:pnp:camera0"), "Selecting the same camera should not trigger a switch.");
        }

        private static void CameraReconnectDelayIsOneSecond()
        {
            AssertEqual("1000", CameraDeviceSelection.ReconnectDelayMs.ToString(), "Camera reconnect delay should be 1 second.");
        }

        private static void AssertEqual(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new Exception(message + Environment.NewLine
                    + "Expected:" + Environment.NewLine + expected + Environment.NewLine
                    + "Actual:" + Environment.NewLine + actual);
            }
        }

        private static void AssertTrue(bool value, string message)
        {
            if (!value)
            {
                throw new Exception(message);
            }
        }
    }
}
