using Hebron;
using Hebron.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace StbSharp.StbImage.Generator
{
	class Program
	{
		private static readonly Dictionary<string, string[]> _outputs = new Dictionary<string, string[]>
		{
			["Bmp"] = new string[]
			{
				"stbi__bmp",
			},
			["Gif"] = new string[]
			{
				"stbi__gif",
				"stbi__load_gif",
				"stbi__process_gif",
			},
			["Jpg"] = new string[]
			{
				"stbi__resample",
				"stbi__huffman",
				"STBI__ZFAST_BITS",
				"stbi__bmask",
				"stbi__jbias",
				"stbi__jpeg_dezigzag",
				"stbi__build_huffman",
				"stbi__build_fast_ac",
				"resample_row",
				"stbi__idct",
				"stbi__jpeg",
				"stbi__YCbCr",
			},
			["Png"] = new string[]
			{
				"STBI__F",
				"stbi__png",
				"first_row",
				"stbi__depth_scale",
				"png_sig",
				"stbi__check_png",
				"stbi__get_chunk_header",
			},
			["Tga"] = new string[]
			{
				"stbi__tga"
			},
			["Zlib"] = new string[]
			{
				"stbi__zhuffman",
				"stbi__zlength",
				"stbi__zdist",
				"stbi__zdefault",
				"length_dezigzag",
				"stbi__zbuild",
				"stbi_zlib",
				"stbi__zbuf"
			},
			["Psd"] = new string[]
			{
				"stbi__psd_decode_rle",
				"stbi__psd"
			},
			["Hdr"] = new string[]
			{
				"stbi__hdr"
			}
		};

		private static void Write<T>(Dictionary<string, T> input, Dictionary<string, string> output) where T : SyntaxNode
		{
			var keys = (from string k in input.Keys orderby k select k).ToArray();
			foreach (var key in keys)
			{
				string outputKey = null;
				foreach (var pair2 in _outputs)
				{
					foreach (var prefix in pair2.Value)
					{
						if (key.StartsWith(prefix))
						{
							outputKey = pair2.Key;
							goto found;
						}
					}
				}
			found:

				string value;
				using (var sw = new StringWriter())
				{
					input[key].NormalizeWhitespace().WriteTo(sw);

					value = sw.ToString();
					value += Environment.NewLine;
				}

				if (outputKey == null)
				{
					if (value.Contains("(stbi__jpeg "))
					{
						outputKey = "Jpg";
					}
					else if (value.Contains("(stbi__zbuf"))
					{
						outputKey = "Zlib";
					}
					else if (value.Contains("(stbi__png "))
					{
						outputKey = "Png";
					}
					else if (value.Contains("(stbi__gif "))
					{
						outputKey = "Gif";
					}
					else if (value.Contains("(stbi__hdr "))
					{
						outputKey = "Hdr";
					}
				}

				if (outputKey == null)
				{
					outputKey = "Common";
				}

				if (!output.ContainsKey(outputKey))
				{
					output[outputKey] = string.Empty;
				}

				output[outputKey] += value;
			}
		}

		private static string PostProcess(string data)
		{
			data = data.Replace("stbi__jpeg j = (stbi__jpeg)(stbi__malloc((ulong)(sizeof(stbi__jpeg))))",
				"var j = new stbi__jpeg()");
			return data;
		}

		static void Process()
		{
			var parameters = new RoslynConversionParameters
			{
				InputPath = @"stb_image.h",
				Defines = new[]
				{
					"STBI_NO_SIMD",
					"STBI_NO_PIC",
					"STBI_NO_PNM",
					"STBI_NO_STDIO",
					"STB_IMAGE_IMPLEMENTATION",
				},
				SkipStructs = new[]
				{
					"stbi_io_callbacks",
					"stbi__context",
				},
				SkipGlobalVariables = new[]
				{
					"stbi__g_failure_reason",
					"stbi__vertically_flip_on_load",
					"stbi__parse_png_file_invalid_chunk",
				},
				SkipFunctions = new[]
				{
					"stbi_failure_reason",
					"stbi_image_free",
					"stbi__err",
					"stbi_is_hdr_from_memory",
					"stbi_is_hdr_from_callbacks",
					"stbi__pnm_isspace",
					"stbi__pnm_skip_whitespace",
					"stbi__pic_is4",
					"stbi__start_mem",
					"stbi__start_callbacks",
					"stbi__rewind",
					"stbi_load_16_from_callbacks",
					"stbi_load_from_callbacks",
					"stbi__get8",
					"stbi__refill_buffer",
					"stbi__at_eof",
					"stbi__skip",
					"stbi__getn",
					"stbi_load_16_from_memory",
					"stbi_load_from_memory",
					"stbi_load_gif_from_memory",
					"stbi_info_from_memory",
					"stbi_info_from_callbacks",
					"stbi_is_16_bit_from_memory",
					"stbi_is_16_bit_from_callbacks",
					"stbi_loadf_from_memory",
					"stbi_loadf_from_callbacks"
				}
			};

			//            var result = TextCodeConverter.Convert(parameters.InputPath, parameters.Defines);

			var result = RoslynCodeConverter.Convert(parameters);

			// Post processing
			Logger.Info("Post processing...");

			var outputFiles = new Dictionary<string, string>();
			Write(result.NamedEnums, outputFiles);
			Write(result.UnnamedEnumValues, outputFiles);
			Write(result.GlobalVariables, outputFiles);
			Write(result.Delegates, outputFiles);
			Write(result.Structs, outputFiles);
			Write(result.Functions, outputFiles);

			foreach (var pair in outputFiles)
			{
				var data = PostProcess(pair.Value);

				var sb = new StringBuilder();
				sb.AppendLine(string.Format("// Generated by Sichem at {0}", DateTime.Now));
				sb.AppendLine();

				sb.AppendLine("using System;");
				sb.AppendLine("using System.Runtime.InteropServices;");
				sb.AppendLine("using Hebron.Runtime;");

				sb.AppendLine();

				sb.Append("namespace StbImageSharp\n{\n\t");
				sb.AppendLine("unsafe partial class StbImage\n\t{");

				data = sb.ToString() + data;
				data += "}\n}";

				File.WriteAllText(@"..\..\..\..\..\..\src\StbImage.Generated." + pair.Key + ".cs", data);
			}
		}

		static void Main(string[] args)
		{
			try
			{
				Process();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
			}

			Console.WriteLine("Finished. Press any key to quit.");
		}
	}
}