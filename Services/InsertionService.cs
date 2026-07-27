using System.Runtime.InteropServices;
using WhisperFlowLocal.Interop;

namespace WhisperFlowLocal.Services;

public class InsertionService(FocusService focusService)
{
    public bool Insert(string text)
    {
        if (string.IsNullOrEmpty(text)) return true;

        focusService.RestoreFocus();

        var inputs = BuildInputArray(text);
        uint sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.INPUT>());
        return sent == (uint)inputs.Length;
    }

    public static NativeMethods.INPUT[] BuildInputArray(string text)
    {
        if (string.IsNullOrEmpty(text)) return [];

        var inputs = new NativeMethods.INPUT[text.Length * 2];
        for (int i = 0; i < text.Length; i++)
        {
            ushort scan = text[i];
            inputs[i * 2] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                U = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = scan,
                        dwFlags = NativeMethods.KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            inputs[i * 2 + 1] = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                U = new NativeMethods.InputUnion
                {
                    ki = new NativeMethods.KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = scan,
                        dwFlags = NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }
        return inputs;
    }
}
