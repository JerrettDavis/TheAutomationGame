using Stride.Input;

namespace Automation.Client.Stride;

public static class StrideKeyboardAdapter
{
    public static Keys ToStrideKey(KeyboardKey key) => key switch
    {
        KeyboardKey.A => Keys.A, KeyboardKey.B => Keys.B, KeyboardKey.C => Keys.C, KeyboardKey.D => Keys.D,
        KeyboardKey.E => Keys.E, KeyboardKey.F => Keys.F, KeyboardKey.G => Keys.G, KeyboardKey.H => Keys.H,
        KeyboardKey.I => Keys.I, KeyboardKey.J => Keys.J, KeyboardKey.K => Keys.K, KeyboardKey.L => Keys.L,
        KeyboardKey.M => Keys.M, KeyboardKey.N => Keys.N, KeyboardKey.O => Keys.O, KeyboardKey.P => Keys.P, KeyboardKey.Q => Keys.Q,
        KeyboardKey.R => Keys.R, KeyboardKey.S => Keys.S, KeyboardKey.T => Keys.T, KeyboardKey.U => Keys.U,
        KeyboardKey.V => Keys.V, KeyboardKey.W => Keys.W, KeyboardKey.X => Keys.X, KeyboardKey.Y => Keys.Y,
        KeyboardKey.Z => Keys.Z,
        KeyboardKey.Digit1 => Keys.D1, KeyboardKey.Digit2 => Keys.D2, KeyboardKey.Digit3 => Keys.D3,
        KeyboardKey.Digit4 => Keys.D4, KeyboardKey.Digit5 => Keys.D5, KeyboardKey.Digit6 => Keys.D6,
        KeyboardKey.Digit7 => Keys.D7,
        KeyboardKey.Digit8 => Keys.D8,
        KeyboardKey.Digit9 => Keys.D9,
        KeyboardKey.Left => Keys.Left, KeyboardKey.Right => Keys.Right, KeyboardKey.Up => Keys.Up, KeyboardKey.Down => Keys.Down,
        KeyboardKey.Enter => Keys.Enter, KeyboardKey.Space => Keys.Space, KeyboardKey.Escape => Keys.Escape,
        KeyboardKey.Tab => Keys.Tab, KeyboardKey.Backspace => Keys.Back, KeyboardKey.Home => Keys.Home,
        KeyboardKey.F1 => Keys.F1, KeyboardKey.F2 => Keys.F2, KeyboardKey.F3 => Keys.F3, KeyboardKey.F4 => Keys.F4,
        KeyboardKey.F5 => Keys.F5, KeyboardKey.F6 => Keys.F6, KeyboardKey.F7 => Keys.F7, KeyboardKey.F8 => Keys.F8,
        KeyboardKey.F9 => Keys.F9, KeyboardKey.F10 => Keys.F10, KeyboardKey.F11 => Keys.F11, KeyboardKey.F12 => Keys.F12,
        _ => throw new ArgumentOutOfRangeException(nameof(key)),
    };
}
