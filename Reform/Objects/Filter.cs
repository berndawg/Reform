// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Reform.Enum;

namespace Reform.Objects
{
    public class Filter
    {
        #region Properties

        [DataMember]
        public string PropertyName { get; set; }

        [DataMember]
        public Operator Operator { get; set; }

        [DataMember]
        public object PropertyValue { get; set; }

        [DataMember]
        public Relationship? Relationship { get; set; }

        [DataMember]
        public Filter LeftChild { get; set; }

        [DataMember]
        public Filter RightChild { get; set; }

        #endregion

        #region Constructors

        public Filter()
        {
        }

        public Filter(string propertyName, Operator op)
        {
            if (op != Operator.IsNull && op != Operator.IsNotNull)
                throw new ArgumentException(string.Format("Don't call this constructor with operator '{0}'", op));

            PropertyName = propertyName;
            Operator = op;
            PropertyValue = null;
        }

        public Filter(string propertyName, Operator op, object propertyValue)
        {
            if (op == Operator.IsNull || op == Operator.IsNotNull)
                throw new ArgumentException(string.Format("Don't call this constructor with operator '{0}'", op));

            PropertyName = propertyName;
            Operator = op;
            PropertyValue = propertyValue;
        }

        #endregion

        #region Operators

        public static Filter operator !(Filter a)
        {
            if (a == null)
                return null;

            return new Filter
            {
                Relationship = Enum.Relationship.Not,
                LeftChild = a
            };
        }

        public static Filter operator &(Filter a, Filter b)
        {
            if (a != null && b != null)
            {
                return new Filter
                {
                    Relationship = Enum.Relationship.And,
                    LeftChild = a,
                    RightChild = b
                };
            }

            return a ?? b;
        }

        public static Filter operator |(Filter a, Filter b)
        {
            if (a != null && b != null)
            {
                return new Filter
                {
                    Relationship = Enum.Relationship.Or,
                    LeftChild = a,
                    RightChild = b
                };
            }

            return a ?? b;
        }

        #endregion

        #region Static Methods

        public static Filter In<T>(string key, List<T> values)
        {
            return In(key, values.ConvertAll(x => (object) x).ToArray());
        }

        public static Filter In(string key, params object[] values)
        {
            return new Filter(key, Operator.In, values);
        }

        public static Filter NotIn<T>(string key, List<T> values)
        {
            return NotIn(key, values.ConvertAll(x => (object) x).ToArray());
        }

        public static Filter NotIn(string key, params object[] values)
        {
            return new Filter(key, Operator.NotIn, values);
        }

        public static Filter EqualTo(string key, object value)
        {
            return new Filter(key, Operator.EqualTo, value);
        }

        public static Filter NotEqualTo(string key, object value)
        {
            return new Filter(key, Operator.NotEqualTo, value);
        }

        public static Filter Like(string key, object value)
        {
            return new Filter(key, Operator.Like, value);
        }

        public static Filter NotLike(string key, object value)
        {
            return new Filter(key, Operator.NotLike, value);
        }

        public static Filter StartsWith(string key, object value)
        {
            return new Filter(key, Operator.Like, $"{value}%");
        }

        public static Filter EndsWith(string key, object value)
        {
            return new Filter(key, Operator.Like, $"%{value}");
        }

        public static Filter Contains(string key, object value)
        {
            return new Filter(key, Operator.Like, $"%{value}%");
        }

        public static Filter GreaterThan(string key, object value)
        {
            return new Filter(key, Operator.GreaterThan, value);
        }

        public static Filter GreaterThanOrEqualTo(string key, object value)
        {
            return new Filter(key, Operator.GreaterThanOrEqualTo, value);
        }

        public static Filter LessThan(string key, object value)
        {
            return new Filter(key, Operator.LessThan, value);
        }

        public static Filter LessThanOrEqualTo(string key, object value)
        {
            return new Filter(key, Operator.LessThanOrEqualTo, value);
        }

        public static Filter IsNull(string key)
        {
            return new Filter(key, Operator.IsNull);
        }

        public static Filter IsNotNull(string key)
        {
            return new Filter(key, Operator.IsNotNull);
        }

        #endregion
    }
}