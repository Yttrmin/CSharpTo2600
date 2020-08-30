﻿//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// A helper class to parse and present strongly-typed options from the 
    /// command-line.
    /// </summary>
    public sealed class Options
    {
        #region Subclasses

        /// <summary>
        /// Represents a strongly-typed listing options element.
        /// </summary>
        public sealed class Listing
        {
            #region Constructors
            /// <summary>
            /// Constructs a new instance of a listing options element.
            /// </summary>
            /// <param name="labelPath">The label filename.</param>
            /// <param name="listPath">The list filename.</param>
            /// <param name="noAssembly">The no-assembly flag.</param>
            /// <param name="noDisassembly">The no-disassembly flag.</param>
            /// <param name="noSource">The no-source flag.</param>
            /// <param name="verbose">The verbose listing flag.</param>
            public Listing(string labelPath,
                           string listPath,
                           bool noAssembly,
                           bool noDisassembly,
                           bool noSource,
                           bool verbose)
            {
                LabelFile = labelPath ?? string.Empty;
                ListingFile = listPath ?? string.Empty;
                NoAssembly = noAssembly;
                NoDisassembly = noDisassembly;
                NoSource = noSource;
                VerboseList = verbose;
            }
            #endregion

            #region Properties

            /// <summary>
            /// Gets the label filename.
            /// </summary>
            public string LabelFile { get; }

            /// <summary>
            /// Gets the list filename.
            /// </summary>
            public string ListingFile { get; }

            /// <summary>
            /// Gets the no-assembly flag.
            /// </summary>
            public bool NoAssembly { get; }

            /// <summary>
            /// Gets the no-disassembly flag.
            /// </summary>
            public bool NoDisassembly { get; }

            /// <summary>
            /// Gets the no-source flag.
            /// </summary>
            public bool NoSource { get; }

            /// <summary>
            /// Gets the verbose list flag.
            /// </summary>
            public bool VerboseList { get; }

            #endregion
        }

        /// <summary>
        /// Represents a strongly-typed logging options element.
        /// </summary>
        public sealed class Logging
        {
            #region Constructors

            /// <summary>
            /// Constructs a new instance of a logging options element.
            /// </summary>
            /// <param name="errorPath">The error filename.</param>
            /// <param name="checksum">The checksum flag.</param>
            /// <param name="noWarnings">The no-warnings flag.</param>
            /// <param name="quietMode">The quiet flag.</param>
            /// <param name="warningsAsErrors">The warnings-as-errors flag.</param>
            /// <param name="warnLeft">The warn left flag.</param>
            public Logging(string errorPath,
                           bool checksum,
                           bool noWarnings,
                           bool quietMode,
                           bool warningsAsErrors,
                           bool warnLeft)
            {
                ErrorFile = errorPath ?? string.Empty;
                Checksum = checksum;
                NoWarnings = noWarnings;
                Quiet = quietMode;
                WarningsAsErrors = warningsAsErrors;
                WarnLeft = warnLeft;
            }
            #endregion

            #region Properties

            /// <summary>
            /// Gets the checksum flag.
            /// </summary>
            public bool Checksum { get; }

            /// <summary>
            /// Gets the error filename.
            /// </summary>
            public string ErrorFile { get; }

            /// <summary>
            /// Gets the no-warnings flag.
            /// </summary>
            public bool NoWarnings { get; }

            /// <summary>
            /// Gets the quiet flag.
            /// </summary>
            public bool Quiet { get; }

            /// <summary>
            /// Gets the warnings-as-errors flag.
            /// </summary>
            public bool WarningsAsErrors { get; }

            /// <summary>
            /// Gets the warn-left flag.
            /// </summary>
            public bool WarnLeft { get; }

            #endregion
        }

        /// <summary>
        /// Represents a strongly-typed defined section element.
        /// </summary>
        public sealed class Section
        {
            #region Constructors

            /// <summary>
            /// Constructs a new instance of a section element.
            /// </summary>
            /// <param name="name">The section name.</param>
            /// <param name="starts">The section starting address, as a string.</param>
            /// <param name="ends">The section ending address, as a string.</param>
            [JsonConstructor]
            public Section(string name, string starts, string ends)
            {
                Name = name;
                Starts = starts;
                Ends = ends;
            }
            #endregion

            #region Methods

            public override string ToString()
            {
                var asString = new List<string> { Name ?? string.Empty };
                if (!string.IsNullOrEmpty(Starts))
                {
                    asString.Add(Starts);
                    if (!string.IsNullOrEmpty(Ends))
                        asString.Add(Ends);
                }
                return string.Join(',', asString);
            }
            #endregion

            #region Properties

            /// <summary>
            /// Gets the section starting address.
            /// </summary>
            public string Starts { get; }

            /// <summary>
            /// Gets the section ending address.
            /// </summary>
            public string Ends { get; }

            /// <summary>
            /// Gets the section name.
            /// </summary>
            public string Name { get; }

            #endregion
        }

        /// <summary>
        /// Represents a strongly-typed target options element.
        /// </summary>
        public sealed class Target
        {
            /// <summary>
            /// Constructs a new instance of the target options element.
            /// </summary>
            /// <param name="binaryFormat">The format.</param>
            /// <param name="cpu">The CPU.</param>
            public Target(string binaryFormat, string cpu)
            {
                Format = binaryFormat ?? string.Empty;
                Cpu = cpu ?? string.Empty;
            }

            /// <summary>
            /// Gets the format.
            /// </summary>
            public string Format { get; }

            /// <summary>
            /// Gets the CPU.
            /// </summary>
            public string Cpu { get; }
        }

        #endregion

        #region Members

        readonly bool _werror;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of the Options class.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="noAssembly">The no-assembly flag.</param>
        /// <param name="caseSensitive">The case-sensitive flag.</param>
        /// <param name="createConfig">The create-config option.</param>
        /// <param name="cpu">The cpu option.</param>
        /// <param name="showChecksums">The show checksums flag.</param>
        /// <param name="configFile">The config filename.</param>
        /// <param name="labelDefines">The label defines.</param>
        /// <param name="noDisassembly">The no-disassembly flag.</param>
        /// <param name="sections">The defined sections.</param>
        /// <param name="errorFile">The error filename.</param>
        /// <param name="format">The format.</param>
        /// <param name="includePath">The include path.</param>
        /// <param name="ignoreColons">The ignore-colons flag.</param>
        /// <param name="labelFile">The label filename.</param>
        /// <param name="listingFile">The listing filename.</param>
        /// <param name="outputFile">The output filename.</param>
        /// <param name="quiet">The quiet mode flag.</param>
        /// <param name="noSource">The no-source flag.</param>
        /// <param name="verboseList">The verbose listing flag.</param>
        /// <param name="noWarnings">The no warnings flag.</param>
        /// <param name="warningsAsErrors">The warnings as errors flag.</param>
        /// <param name="warnLeft">The warn left flag.</param>
        public Options(IList<string> inputFiles,
                       bool noAssembly,
                       bool caseSensitive,
                       string createConfig,
                       string cpu,
                       bool showChecksums,
                       string configFile,
                       IList<string> labelDefines,
                       bool noDisassembly,
                       IList<string> sections,
                       string errorFile,
                       string format,
                       string includePath,
                       bool ignoreColons,
                       string labelFile,
                       string listingFile,
                       string outputFile,
                       bool quiet,
                       bool noSource,
                       bool verboseList,
                       bool noWarnings,
                       bool warningsAsErrors,
                       bool warnLeft) : this(inputFiles,
                                             new Listing(labelFile,
                                                         listingFile,
                                                         noAssembly,
                                                         noDisassembly,
                                                         noSource,
                                                         verboseList),
                                             new Logging(errorFile,
                                                         showChecksums,
                                                         noWarnings,
                                                         quiet,
                                                         warningsAsErrors,
                                                         warnLeft),
                                             null,
                                             new Target(format, cpu),
                                             labelDefines,
                                             caseSensitive,
                                             outputFile,
                                             includePath,
                                             ignoreColons)
        {
            if (!string.IsNullOrEmpty(configFile))
            {
                ConfigFile = configFile;
                OutputFile = string.Empty;
            }
            else
            {
                ConfigFile = string.Empty;
            }
            if (createConfig != null)
                CreateConfig = createConfig;
            if (sections != null)
                Sections = new ReadOnlyCollection<string>(sections);
        }

        /// <summary>
        /// Constructs an instance of the Options class.
        /// </summary>
        /// <param name="listingOptions">The listing options.</param>
        /// <param name="loggingOptions">The logging options.</param>
        /// <param name="sections">The sections.</param>
        /// <param name="target">The target options.</param>
        /// <param name="defines">The defines.</param>
        /// <param name="sources">The input files.</param>
        /// <param name="caseSensitive">The case-sensitive flag.</param>
        /// <param name="outputFile">The output filename.</param>
        /// <param name="includePath">The include path.</param>
        /// <param name="ignoreColons">The ignore-colons flag.</param>
        [JsonConstructor]
        public Options(IList<string> sources,
                       Listing listingOptions,
                       Logging loggingOptions,
                       IList<Section> sections,
                       Target target,
                       IList<string> defines,
                       bool caseSensitive,
                       string outputFile,
                       string includePath,
                       bool ignoreColons)
        {
            if (listingOptions != null)
            {
                ListingFile = listingOptions.ListingFile;
                LabelFile = listingOptions.LabelFile;
                NoAssembly = listingOptions.NoAssembly;
                NoDisassembly = listingOptions.NoDisassembly;
                NoSource = listingOptions.NoSource;
                VerboseList = listingOptions.VerboseList;
            }
            else
            {
                ListingFile = string.Empty;
                LabelFile = string.Empty;
            }
            if (loggingOptions != null)
            {
                ErrorFile = loggingOptions.ErrorFile;
                NoWarnings = loggingOptions.NoWarnings;
                WarnLeft = loggingOptions.WarnLeft;
                _werror = loggingOptions.WarningsAsErrors;
                Quiet = loggingOptions.Quiet;
                ShowChecksums = loggingOptions.Checksum;
            }
            else
            {
                ErrorFile = string.Empty;
            }
            Format = target == null ? string.Empty : target.Format;
            CPU = target == null ? string.Empty : target.Cpu;
            CaseSensitive = caseSensitive;

            OutputFile = outputFile ?? "a.out";
            ConfigFile = string.Empty;
            IncludePath = includePath ?? string.Empty;
            IgnoreColons = ignoreColons;

            LabelDefines = defines == null ? new List<string>().AsReadOnly() : new ReadOnlyCollection<string>(defines);
            InputFiles = sources == null ? new List<string>().AsReadOnly() : new ReadOnlyCollection<string>(sources);

            if (sections != null)
                Sections = new ReadOnlyCollection<string>(new List<string>(sections.Select(s => s.ToString())));
            else
                Sections = new List<string>().AsReadOnly();
            CreateConfig = null;
        }

        #endregion

        #region Methods

        string JsonSerialize()
        {
            var root = new JObject();
            var logging = new JObject();
            var listing = new JObject();
            var target = new JObject();
            var defines = new JArray();
            var inputfiles = new JArray();
            var sections = new JArray();


            if (CaseSensitive)
                root.Add("caseSensitive", true);
            if (LabelDefines.Count > 0)
            {
                foreach (var d in LabelDefines)
                    defines.Add(d);
                root.Add("defines", defines);
            }
            if (IgnoreColons)
                root.Add("ignoreColons", true);
            if (!string.IsNullOrEmpty(IncludePath))
                root.Add("includePath", IncludePath);

            if (!string.IsNullOrEmpty(LabelFile))
                listing.Add("labelPath", LabelFile);
            if (!string.IsNullOrEmpty(ListingFile))
                listing.Add("listPath", ListingFile);
            if (NoAssembly)
                listing.Add("noAssembly", true);
            if (NoDisassembly)
                listing.Add("noDisassembly", true);
            if (NoSource)
                listing.Add("noSource", true);
            if (VerboseList)
                listing.Add("verbose", true);

            if (listing.Count > 0)
                root.Add("listingOptions", listing);

            if (ShowChecksums)
                logging.Add("checksum", true);
            if (!string.IsNullOrEmpty(ErrorFile))
                logging.Add("errorPath", ErrorFile);
            if (NoWarnings)
                logging.Add("noWarnings", true);
            if (_werror)
                logging.Add("WarningsAsErrors", true);
            if (WarnLeft)
                logging.Add("warnLeft", true);

            if (logging.Count > 0)
                root.Add("loggingOptions", logging);

            if (!string.IsNullOrEmpty(OutputFile) && !OutputFile.Equals("a.out"))
                root.Add("outputFile", OutputFile);
            if (InputFiles.Count > 0)
            {
                foreach (var i in InputFiles)
                    inputfiles.Add(i);
                root.Add("sources", inputfiles);
            }
            if (Sections.Count > 0)
            {
                foreach (var s in Sections)
                {
                    var sParms = s.Split(',');
                    if (sParms.Length == 3)
                    {
                        var section = new JObject
                        {
                            { "name",   sParms[0] },
                            { "starts", sParms[1] },
                            { "ends",   sParms[2] }
                        };
                        sections.Add(section);
                    }
                    else
                    {
                        throw new Exception("Invalid argument for option '.dsection'.");
                    }
                }
                if (sections.Count > 0)
                    root.Add("sections", sections);
            }
            if (!string.IsNullOrEmpty(CPU) || !string.IsNullOrEmpty(Format))
            {
                target.Add("binaryFormat", Format);
                target.Add("cpu", CPU);
                root.Add("target", target);
            }
            return root.ToString();
        }

        static void CreateConfigFile(Options o)
        {
            var config = o.CreateConfig;
            var typeName = string.Empty;
            var jsonFormatted = string.Empty;
            var json = string.Empty;
            switch (config)
            {
                case "m":
                case "min":
                    json = ConfigConstants.CONFIG_MIN;
                    typeName = "-min";
                    break;
                case "f":
                case "full":
                    json = ConfigConstants.CONFIG_FULL;
                    typeName = "-full";
                    break;
                case "s":
                case "schema":
                    json = ConfigConstants.CONFIG_SCHEMA;
                    typeName = "-schema";
                    break;
                case "a":
                case "args":
                    jsonFormatted = o.JsonSerialize();
                    break;
                default:
                    throw new Exception($"Invalid argument for option --createconfig: {config}.");
            }
            if (string.IsNullOrEmpty(jsonFormatted))
            {
                using (var stringReader = new StringReader(json))
                using (var stringWriter = new StringWriter())
                {
                    var jsonReader = new JsonTextReader(stringReader);
                    var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                    jsonWriter.WriteToken(jsonReader);
                    jsonFormatted = stringWriter.ToString();
                }
            }
            File.WriteAllText($"config{typeName}.json", jsonFormatted);
            throw new Exception($"Config file \"config{typeName}.json\" created."); 
        }

        static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            var heading = $"{Assembler.AssemblerNameSimple}\n{Assembler.AssemblerVersion}";
            if (errs.IsVersion())
                throw new Exception(heading);
            if (errs.IsHelp())
            {
                var helpText = HelpText.AutoBuild(result, h =>
                {
                    h.AdditionalNewLineAfterOption = false;
                    h.AddEnumValuesToHelpText = false;
                    h.AddPostOptionsLine("To log a defect, go to https://github.com/informedcitizenry/6502.Net/issues");
                    h.Heading = heading;
                    h.Copyright = string.Empty;
                    return HelpText.DefaultParsingErrorsHandler(result, h);
                }, e => e);
                var ht = helpText.ToString().Replace("(pos. 0)", "        ");
                throw new Exception(ht);
            }
            throw new Exception("Invalid arguments. Try '--help' for usage.");
        }

        /// <summary>
        /// Parses and fills an <see cref="Options"/>Options object from 
        /// passed command-line arguments.
        /// </summary>
        /// <param name="args">A collection of arguments to parse as arguments.</param>
        /// <returns>Returns an instance of the Options class.</returns>
        public static Options FromArgs(IEnumerable<string> args)
        {
            try
            {
                Options options = null;
                var parser = new Parser(with =>
                {
                    with.HelpWriter = null;
                });
                var result = parser.ParseArguments<Options>(args);
                _ = result.WithParsed(o =>
                {
                    if (string.IsNullOrEmpty(o.ConfigFile))
                    {
                        options = o;
                    }
                    else
                    {
                        var parseErrs = new HashSet<string>();

                        var confIx = args.ToList().FindIndex(s => s.StartsWith("--config", StringComparison.Ordinal));
                        if (confIx != 0 || confIx != args.ToList().FindLastIndex(s => s.StartsWith('-')))
                            Console.WriteLine("Option --config ignores all other options.");

                        var configJson = File.ReadAllText(o.ConfigFile);
                        var reader = new JsonTextReader(new StringReader(configJson));

#pragma warning disable CS0618 // Type or member is obsolete
                        var validatingReader = new JsonValidatingReader(reader)

                        {
                            Schema = JsonSchema.Parse(ConfigConstants.CONFIG_SCHEMA)
                        };
#pragma warning restore CS0618 // Type or member is obsolete
                        validatingReader.ValidationEventHandler += (obj, a) => parseErrs.Add(a.Message);

                        var serializer = new JsonSerializer();
                        options = serializer.Deserialize<Options>(validatingReader);
                        if (parseErrs.Count > 0)
                            throw new Exception($"One or more errors in config file:\n{string.Join(Environment.NewLine, parseErrs)}");
                    }
                })
                .WithNotParsed(errs => DisplayHelp(result, errs));
                if (options.CreateConfig != null)
                    CreateConfigFile(options);
                return options;
            }
            catch (Exception ex)
            {
                if (ex is JsonException)
                    throw new Exception($"Error parsing config file: {ex.Message}");
                else
                    throw ex;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the read-only list of input filenames.
        /// </summary>
        [Value(0, Required = false, HelpText = "The source file(s) to assemble", MetaName = "<inputs>")]
        public ReadOnlyCollection<string> InputFiles { get; }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress assembly bytes.
        /// </summary>
        [Option('a', "no-assembly", Required = false, HelpText = "Suppress assembled bytes from listing")]
        public bool NoAssembly { get; }

        /// <summary>
        /// Gets a flag that indicates the source should be processed as
        /// case-sensitive.
        /// </summary>
        [Option('C', "case-sensitive", Required = false, HelpText = "Treat all symbols as case-sensitive")]
        public bool CaseSensitive { get; }

        /// <summary>
        /// Create a config template.
        /// </summary>
        [Option("createconfig", Required = false, HelpText = "Create a config file", MetaValue = "{a|f|m|s}")]
        public string CreateConfig { get; }

        /// <summary>
        /// Gets the selected CPU.
        /// </summary>
        /// <value>The cpu.</value>
        [Option('c', "cpu", Required = false, HelpText = "Specify the target CPU and instruction set", MetaValue = "<arg>")]
        public string CPU { get; }

        /// <summary>
        /// Gets a flag indicating that checksum information should be printed after 
        /// assembly.
        /// </summary>
        [Option("checksum", Required = false, HelpText = "Display checksum information on assembly")]
        public bool ShowChecksums { get; }

        /// <summary>
        /// Gets the config option file.
        /// </summary>
        [Option("config", Required = false, HelpText = "Load all settings from a configuration file", MetaValue = "<file>")]
        public string ConfigFile { get; }

        /// <summary>
        /// Gets the read-only list of label defines.
        /// </summary>
        [Option('D', "define", Separator = ':', Required = false, HelpText = "Assign value to a global label in <args>", MetaValue = "<arg>")]
        public ReadOnlyCollection<string> LabelDefines { get; }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress 6502 disassembly.
        /// </summary>
        [Option('d', "no-dissassembly", Required = false, HelpText = "Suppress disassembly from assembly listing")]
        public bool NoDisassembly { get; }

        /// <summary>
        /// Gets the list of defined sections.
        /// </summary>
        [Option("dsections", Required = false, HelpText = "Define one or more sections", MetaValue = "<sections>")]
        public ReadOnlyCollection<string> Sections { get; }

        /// <summary>
        /// Gets the error filename.
        /// </summary>
        [Option('E', "error", Required = false, HelpText = "Dump errors to <file>", MetaValue = "<file>")]
        public string ErrorFile { get; }

        /// <summary>
        /// Gets or sets the target architecture information.
        /// </summary>
        [Option("format", Required = false, HelpText = "Specify binary output format", MetaValue = "<format>")]
        public string Format { get; }

        /// <summary>
        /// Gets the path to search to include in sources.
        /// </summary>
        [Option('I', "include-path", Required = false, HelpText = "Include search path", MetaValue = "<path>")]
        public string IncludePath { get; }

        /// <summary>
        /// Gets a flag indicating that colons in semi-colon comments should be treated
        /// as comments.
        /// </summary>
        [Option("ignore-colons", Required = false, HelpText = "Ignore colons in semi-colon comments")]
        public bool IgnoreColons { get; }

        /// <summary>
        /// Gets the label listing filename.
        /// </summary>
        [Option('l', "labels", Required = false, HelpText = "Output label definitions to <arg>", MetaValue = "<arg>")]
        public string LabelFile { get; }

        /// <summary>
        /// The assembly listing filename.
        /// </summary>
        [Option('L', "list", Required = false, HelpText = "Output listing to <file>", MetaValue = "<file>")]
        public string ListingFile { get; }

        /// <summary>
        /// Gets the output filename.
        /// </summary>
        [Option('o', "output", Required = false, HelpText = "Output assembly to <file>", MetaValue = "<file>")]
        public string OutputFile { get; }

        /// <summary>
        /// Gets the flag that indicates assembly should be quiet.
        /// </summary>
        [Option('q', "Quiet", Required = false, HelpText = "Assemble in quiet mode (no console)")]
        public bool Quiet { get; }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress original source.
        /// </summary>
        [Option('s', "no-source", Required = false, HelpText = "Suppress original source from listing")]
        public bool NoSource { get; }

        /// <summary>
        /// Gets a flag indicating that assembly listing should be 
        /// verbose.
        /// </summary>
        [Option("verbose-asm", Required = false, HelpText = "Include all directives/comments in listing")]
        public bool VerboseList { get; }

        /// <summary>
        /// Gets the flag that indicates warnings should be suppressed.
        /// </summary>
        [Option('w', "no-warn", Required = false, HelpText = "Suppress all warnings")]
        public bool NoWarnings { get; }

        /// <summary>
        /// Gets a flag that treats warnings as errors.
        /// </summary>
        [Option("werror", Required = false, HelpText = "Treat all warnings as errors")]
        public bool WarningsAsErrors => !NoWarnings && _werror;

        /// <summary>
        /// Gets a value indicating whether to suppress warnings for whitespaces 
        /// before labels.
        /// </summary>
        /// <value>If <c>true</c> warn left; otherwise, suppress the warning.</value>
        [Option("wleft", Required = false, HelpText = "Warn when a whitespace precedes a label")]
        public bool WarnLeft { get; }

        [Usage(ApplicationAlias = "6502.Net.exe")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("General", new UnParserSettings() { PreferShortName = true }, new Options(new string[] { "inputfile.asm" }, null, null, null, null, null, false, "output.bin", null, false));
                yield return new Example("From Config", new Options(null, false, false, null, null, false, "config.json", null, false, null, null, null, null, false, null, null, null, false, false, false, false, false, false));
            }
        }

        #endregion
    }
}