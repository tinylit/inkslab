namespace Inkslab.Net.Validation
{
    /// <summary>The HTTP entity validation boundary.</summary>
    public enum ValidationStage
    {
        /// <summary>Before request serialization, form expansion or query expansion.</summary>
        Request,
        /// <summary>After deserialization, parsing fallback and business result mapping.</summary>
        Response
    }
}
