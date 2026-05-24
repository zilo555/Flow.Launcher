using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Flow.Launcher.Plugin.Program.Logger;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Variant;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.PropertiesSystem;
using Windows.Win32.Storage.FileSystem;

namespace Flow.Launcher.Plugin.Program.Programs
{
    public class ShellLinkHelper
    {
        // To initialize the app description
        public string description = string.Empty;
        public string arguments = string.Empty;

        // Retrieve the target path using Shell Link
        public unsafe string retrieveTargetPath(string path)
        {
            var link = new ShellLink();
            const int STGM_READ = 0;
            ((IPersistFile)link).Load(path, STGM_READ);
            var hwnd = new HWND(IntPtr.Zero);
            // Use SLR_NO_UI to avoid showing any UI during resolution, like Problem with Shortcut dialogs
            // https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-ishelllinka-resolve
            ((IShellLinkW)link).Resolve(hwnd, (uint)SLR_FLAGS.SLR_NO_UI);

            var data = new WIN32_FIND_DATAW();
            var target = string.Empty;
            try
            {
                Span<char> targetBuffer = stackalloc char[(int)PInvoke.MAX_PATH];
                fixed (char* targetBufferPtr = targetBuffer)
                {
                    ((IShellLinkW)link).GetPath((PWSTR)targetBufferPtr, (int)PInvoke.MAX_PATH, &data, (uint)SLGP_FLAGS.SLGP_SHORTPATH);
                    target = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(targetBufferPtr).ToString();
                }
            }
            catch (COMException e)
            {
                ProgramLogger.LogException($"|IShellLinkW|retrieveTargetPath|{path}" +
                "|Error occurred while getting program target path from shell link", e);
            }

            // To set the app description
            if (!string.IsNullOrEmpty(target))
            {
                description = retrieveDescription((IShellLinkW)link, path);
                arguments = retrieveArguments((IPropertyStore)link, path);
            }

            // To release unmanaged memory
            Marshal.ReleaseComObject(link);

            return target;
        }

        private static unsafe string retrieveDescription(IShellLinkW shellLink, string path)
        {
            try
            {
                Span<char> descriptionBuffer = stackalloc char[(int)PInvoke.INFOTIPSIZE];
                fixed (char* descriptionBufferPtr = descriptionBuffer)
                {
                    shellLink.GetDescription(descriptionBufferPtr, (int)PInvoke.INFOTIPSIZE);
                    return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(descriptionBufferPtr).ToString();
                }
            }
            catch (COMException e)
            {
                // C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\MiracastView.lnk always cause exception
                ProgramLogger.LogException(
                    $"|IShellLinkW|retrieveDescription|{path}" +
                    "|Error caused likely due to trying to get the description of the program",
                    e
                );
                return string.Empty;
            }
        }

        private static string retrieveArguments(IPropertyStore shellLinkPropertyStore, string path)
        {
            PROPVARIANT argumentsProperty = new PROPVARIANT();

            try
            {
                var argumentsKey = PInvoke.PKEY_Link_Arguments;
                shellLinkPropertyStore.GetValue(in argumentsKey, out argumentsProperty);

                // CsWin32 preserves native C unions, so nested union fields are generated as "Anonymous".
                // see structure at https://learn.microsoft.com/en-ie/windows/win32/api/propidlbase/ns-propidlbase-propvariant#syntax
                var propVariantHeader = argumentsProperty.Anonymous.Anonymous;
                var propVariantValueUnion = propVariantHeader.Anonymous;
                var propVariantType = propVariantHeader.vt;
                
                return propVariantType switch
                {
                    VARENUM.VT_EMPTY => string.Empty,
                    VARENUM.VT_LPWSTR => propVariantValueUnion.pwszVal.ToString(),
                    _ => string.Empty
                };
            }
            catch (COMException e)
            {
                ProgramLogger.LogException($"|IShellLinkW|retrieveArguments|{path}|Error occurred while getting program arguments", e);
                return string.Empty;
            }
            finally
            {
                PInvoke.PropVariantClear(ref argumentsProperty);
            }
        }
    }
}
