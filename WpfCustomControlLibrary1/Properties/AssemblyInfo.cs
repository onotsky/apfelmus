using System.Windows;

// WICHTIG: Ohne dieses Attribut findet WPF den Default-Style eines CustomControls in
// Themes/Generic.xaml NICHT -> CloseableTabItem faellt auf ein schlichtes TabItem ohne
// Schliess-Button ("x") zurueck, und neue Such-Tabs sehen nicht "closeable" aus.
// Das Attribut lag frueher in der bei der SDK-Umstellung geloeschten AssemblyInfo.cs; die
// uebrigen Assembly-Metadaten (Version usw.) werden vom SDK aus Directory.Build.props generiert,
// deshalb steht hier bewusst NUR ThemeInfo (sonst gaebe es doppelte Attribute).
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,        // wo themespezifische Ressourcen liegen: keine
    ResourceDictionaryLocation.SourceAssembly // wo generic.xaml liegt: im selben Assembly
)]
