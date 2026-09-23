namespace RPGFramework.Field
{
    /// <summary>
    /// Whether a field is built in 3D or 2D. A field is one or the other, and its entities follow: which kind of
    /// collider and rigidbody they carry, and so which movement driver they get.
    /// </summary>
    internal enum FieldDimension
    {
        ThreeD = 0,

        TwoD = 1
    }
}