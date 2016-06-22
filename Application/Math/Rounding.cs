using System.Collections.Generic;
using System.Linq;

namespace Application.Math.Rounding
{
    public static class Rounding
    {
        public static decimal[] GetRoundedValues(IEnumerable<decimal> listOfValues, decimal actualTotal, byte precision)
        {
            var RoundedValueError = listOfValues.Select(x =>
            {
                var RoundedValue = System.Math.Round(x, precision);
                return new
                {
                    RoundedValue = RoundedValue,
                    Error = x - RoundedValue
                };
            }).ToArray();

            var RoundedTotal = RoundedValueError.Sum(x => x.RoundedValue);
            var RoundingError = actualTotal - RoundedTotal;
            var UnitOfPrecision = new decimal(1, 0, 0, false, precision);
            var NumberOfUnitsToModify = (int)System.Math.Floor(System.Math.Abs(RoundingError) / UnitOfPrecision);

            var AdjustedList = new List<decimal>();
            if (RoundingError > 0)
            {
                foreach (var roundedValue in RoundedValueError)
                {
                    if (roundedValue.Error > 0 && NumberOfUnitsToModify > 0)
                    {
                        NumberOfUnitsToModify--;
                        AdjustedList.Add(roundedValue.RoundedValue + UnitOfPrecision);
                    }
                    else
                    {
                        AdjustedList.Add(roundedValue.RoundedValue);
                    }
                }
                return AdjustedList.ToArray();
            }
            if (RoundingError < 0)
            {
                foreach (var roundValue in RoundedValueError)
                {
                    if (roundValue.Error < 0 && NumberOfUnitsToModify > 0)
                    {
                        NumberOfUnitsToModify--;
                        AdjustedList.Add(roundValue.RoundedValue - UnitOfPrecision);
                    }
                    else
                    {
                        AdjustedList.Add(roundValue.RoundedValue);
                    }
                }
                return AdjustedList.ToArray();
            }
            return RoundedValueError.Select(x => x.RoundedValue).ToArray();
        }
    }
}