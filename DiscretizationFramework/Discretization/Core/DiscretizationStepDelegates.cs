namespace DiscretizationFramework.Discretization.Core
{
    /// <summary>
    /// Delegát, ktorý definuje signatúru pre jeden diskretizačný krok.
    /// Kroky prijímajú DiscretizationContext a vracajú modifikovaný DiscretizationContext.
    /// </summary>
    /// <param name="context">Aktuálny kontext diskretizácie.</param>
    /// <returns>Modifikovaný kontext diskretizácie po vykonaní kroku.</returns>
    public delegate DiscretizationContext DiscretizationStep(DiscretizationContext context);
}