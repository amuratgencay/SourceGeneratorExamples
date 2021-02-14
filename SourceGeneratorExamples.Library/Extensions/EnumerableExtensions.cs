﻿using System;
using System.Collections.Generic;

namespace SourceGeneratorExamples.Library.Extensions
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var element in source) action(element);
        }
    }
}