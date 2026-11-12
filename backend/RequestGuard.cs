using System.Collections.Concurrent;
using System.Reflection;
namespace Society.Api;
public static class RequestGuard {
 static readonly ConcurrentDictionary<Type,PropertyInfo[]> Fields=new();
 static bool Record(Type type)=>type.Namespace=="Society.Api"&&type.GetMethod("<Clone>$")!=null;
 public static void Validate(object value)=>Validate(value,0);
 static void Validate(object value,int depth){if(depth>8)throw new DomainException("Request nesting is too deep");var fields=Fields.GetOrAdd(value.GetType(),type=>type.GetProperties().Where(p=>p.CanRead&&(p.PropertyType==typeof(string)||p.PropertyType.IsArray||Record(p.PropertyType))).ToArray());var context=new NullabilityInfoContext();foreach(var p in fields){var item=p.GetValue(value);var info=context.Create(p);if(item==null){if(info.ReadState==NullabilityState.NotNull)throw new DomainException("Required field cannot be null: "+p.Name);continue;}if(item is Array array){foreach(var element in array){if(element==null){if(info.ElementType?.ReadState==NullabilityState.NotNull)throw new DomainException("Array entries cannot be null: "+p.Name);}else if(Record(element.GetType()))Validate(element,depth+1);}}else if(Record(p.PropertyType))Validate(item,depth+1);}}
}
