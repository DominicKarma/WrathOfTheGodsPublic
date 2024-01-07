namespace NoxusBoss.Core.Graphics.Automators
{
    // This primarily exists for the purpose of ensuring that prims are pixelated so as to be consistent with Terraria's artstyle, but theoretically any projectile can use it.
    public interface IDrawPixelated
    {
        public void DrawWithPixelation();
    }
}
