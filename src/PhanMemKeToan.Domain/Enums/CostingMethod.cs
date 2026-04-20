namespace PhanMemKeToan.Domain.Enums;

public enum CostingMethod
{
    FIFO = 1,
    LIFO = 2,
    /// <summary>Default costing method per BR-IN01.</summary>
    WeightedAverage = 3,
    SpecificIdentification = 4
}
