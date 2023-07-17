namespace Velvet.Async;

[Flags]
public enum VelvetPlatformStepMask : byte
{
    None               = 0x0,
    Initialization     = 0x1,
    PostInitialization = 0x2,
    PreUpdate          = 0x4,
    Update             = 0x8,
    PostUpdate         = 0x10,
    PreRender          = 0x20,
    Render             = 0x40,
    PostRender         = 0x80,
    All                = 0xFF,
}