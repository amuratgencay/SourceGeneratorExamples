using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

#nullable disable

namespace SourceGeneratorExamples.Library.Extensions
{
    internal static class SymbolExtensions
    {
        private static readonly Dictionary<string, string> BuiltinTypeMapping =
            new(StringComparer.OrdinalIgnoreCase)
            {
                {"string", typeof(string).ToString()},
                {"long", typeof(long).ToString()},
                {"int", typeof(int).ToString()},
                {"short", typeof(short).ToString()},
                {"ulong", typeof(ulong).ToString()},
                {"uint", typeof(uint).ToString()},
                {"ushort", typeof(ushort).ToString()},
                {"byte", typeof(byte).ToString()},
                {"double", typeof(double).ToString()},
                {"float", typeof(float).ToString()},
                {"decimal", typeof(decimal).ToString()},
                {"bool", typeof(bool).ToString()}
            };

        public static IEnumerable<IPropertySymbol> GetAllMembers(this INamedTypeSymbol namedTypeSymbol)
        {
            return namedTypeSymbol.GetMembers()
                .Concat(namedTypeSymbol.GetBaseTypes().SelectMany(t => t.GetMembers()))
                .OfType<IPropertySymbol>()
                .Where(m => /*m.DeclaredAccessibility == Accessibility.Public && !m.IsReadOnly &&*/ !m.IsIndexer &&
                    !m.IsStatic);
        }

        public static IMethodSymbol GetConstructor(this INamedTypeSymbol type)
        {
            return type.Constructors
                .Where(c => !c.IsStatic)
                .Aggregate((c1, c2) => c1.Parameters.Length > c2.Parameters.Length ? c1 : c2);
        }

        public static bool IsNullable(this ITypeSymbol type)
        {
            return ((type as INamedTypeSymbol)?.IsGenericType ?? false)
                   && type.OriginalDefinition.ToDisplayString()
                       .Equals("System.Nullable<T>", StringComparison.OrdinalIgnoreCase);
        }


        public static bool IsNullable(this ITypeSymbol type, out ITypeSymbol nullableType)
        {
            if (type.IsNullable())
            {
                nullableType = ((INamedTypeSymbol) type).TypeArguments.First();
                return true;
            }

            nullableType = null;
            return false;
        }

        public static string GetFullName(this INamespaceOrTypeSymbol type)
        {
            if (type is IArrayTypeSymbol arrayType) return $"{arrayType.ElementType.GetFullName()}[]";

            if ((type as ITypeSymbol).IsNullable(out var t)) return $"System.Nullable`1[{t.GetFullName()}]";

            var name = type.ToDisplayString();

            if (!BuiltinTypeMapping.TryGetValue(name, out var output)) output = name;

            return output;
        }

        public static string GetFullMetadataName(this INamespaceOrTypeSymbol symbol)
        {
            ISymbol currentSymbol = symbol;
            var sb = new StringBuilder(currentSymbol.MetadataName);

            var last = currentSymbol;
            currentSymbol = currentSymbol.ContainingSymbol;

            if (currentSymbol == null) return symbol.GetFullName();

            while (currentSymbol != null && !IsRootNamespace(currentSymbol))
            {
                if (currentSymbol is ITypeSymbol && last is ITypeSymbol)
                    sb.Insert(0, '+');
                else
                    sb.Insert(0, '.');
                sb.Insert(0, currentSymbol.MetadataName);

                currentSymbol = currentSymbol.ContainingSymbol;
            }

            var namedType = symbol as INamedTypeSymbol;

            if (!(namedType?.TypeArguments.Any() ?? false)) return sb.ToString();
            var genericArgs = string.Join(",", namedType.TypeArguments.Select(GetFullMetadataName));
            sb.Append($"[{genericArgs}]");

            return sb.ToString();
        }

        private static bool IsRootNamespace(ISymbol s)
        {
            return s is INamespaceSymbol {IsGlobalNamespace: true};
        }

        internal static bool IsAssignableFrom(this ITypeSymbol targetType, ITypeSymbol sourceType,
            bool exactMatch = false)
        {
            if (targetType is null) return false;

            if (exactMatch) return SymbolEqualityComparer.Default.Equals(sourceType, targetType);

            while (sourceType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(sourceType, targetType)) return true;

                if (targetType.TypeKind == TypeKind.Interface)
                    return sourceType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, targetType));

                sourceType = sourceType.BaseType;
            }

            return false;
        }

        internal static IEnumerable<ITypeSymbol> GetBaseTypes(this ITypeSymbol typeSymbol)
        {
            var currentSymbol = typeSymbol;
            while (currentSymbol.BaseType != null && currentSymbol.BaseType.GetFullMetadataName() != "System.Object")
            {
                currentSymbol = currentSymbol.BaseType;
                yield return currentSymbol;
            }
        }
    }
}