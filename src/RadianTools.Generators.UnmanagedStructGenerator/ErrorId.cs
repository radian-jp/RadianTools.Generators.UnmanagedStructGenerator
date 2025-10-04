namespace RadianTools.Generators.UnmanagedStructGenerator;

internal static class ErrorId
{
    // FixedBuffer 系
    public const string UnmanagedType = "FBGE001";
    public const string LengthMustBePositive = "FBGE002";
    public const string MustBePartial = "FBGE003";

    // 将来 NativeHandle 系などを追加する場合はここに追記
}

internal static class WarningId
{
    // FixedBuffer 系
    public const string CharType = "FBGW001";
}
