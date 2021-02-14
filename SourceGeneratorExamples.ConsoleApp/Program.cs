using System;
using SourceGeneratorExamples.Library;

namespace SourceGeneratorExamples.ConsoleApp
{
    [Builder(ordered:true)]
    public partial class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Person(string firstName, string lastName)
        {
            FirstName = firstName;
            LastName = lastName;
        }
        //public override string ToString() => $"{FirstName} {LastName}";
    }

    internal class Program
    {
        private static void Main()
        {
            var person = Person.Builder().WithFirstName("Johnny").WithLastName("Bravo").Build();
            Console.WriteLine(person.ToString());
        }
    }
}