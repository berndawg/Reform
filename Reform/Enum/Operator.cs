// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
namespace Reform.Enum
{
    public enum Operator : byte
    {
        EqualTo,
        NotEqualTo,
        Like,
        NotLike,
        LessThan,
        LessThanOrEqualTo,
        GreaterThan,
        GreaterThanOrEqualTo,
        IsNull,
        IsNotNull,
        In,
        NotIn
    }
}