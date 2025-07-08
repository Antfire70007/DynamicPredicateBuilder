using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicPredicateBuilder.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class QueryableAttribute : Attribute
{
}
