using System.Runtime.CompilerServices;

[module: SkipLocalsInit]

//Extra libraries
[assembly: InternalsVisibleTo("Net.Collections")]

//Tests
[assembly: InternalsVisibleTo("Net.Benchmarks")]
[assembly: InternalsVisibleTo("Net.Communication.Tests")]