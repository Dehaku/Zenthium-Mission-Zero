using System.Runtime.CompilerServices;

// Generated data assets are only ever filled in by the importer.
[assembly: InternalsVisibleTo("Zenthium.Editor")]

// Approved exception to writing-tests.md (Milestone 4): tests build module and hero data through the same
// Populate methods the importer uses, since generated assets have no other way to be filled in.
[assembly: InternalsVisibleTo("Zenthium.Tests.EditMode")]
