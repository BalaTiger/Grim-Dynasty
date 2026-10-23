using System.ComponentModel;

namespace GrimDynasty.StaticAnalysis.DiagnosticRune13b268184764304bd0a6fe1bc63f3de.ImageUnknown
{
    // Test-only fault constructors. These are compiled beside a namespace-renamed
    // copy of ReaOwnedJob, never referenced by the production sampler or assembly.
    // Each derived source contains one fixed call; no production fault switch,
    // environment setting, callback registration, or arbitrary error-code API exists.
    public static class OwnedJobDiagnosticFixture
    {
        public static int ImageFaultCount { get; private set; }
        public static int ExitFaultCount { get; private set; }

        public static string FailImage5()
        {
            ImageFaultCount++;
            throw new Win32Exception(5, "QueryFullProcessImageName");
        }

        public static string FailImage87()
        {
            ImageFaultCount++;
            throw new Win32Exception(87, "QueryFullProcessImageName");
        }

        public static string FailImageUnknown()
        {
            ImageFaultCount++;
            throw new Win32Exception(123456, "QueryFullProcessImageName");
        }

        public static bool FailExitCheck6()
        {
            ExitFaultCount++;
            throw new Win32Exception(6, "WaitForSingleObject");
        }
    }
}
