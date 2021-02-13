using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sichem;

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

		private static void Write(Dictionary<string, string> input, Dictionary<string, string> output)
		{
			foreach (var pair in input)
			{
				string outputKey = null;
				foreach (var pair2 in _outputs)
				{
					foreach (var prefix in pair2.Value)
					{
						if (pair.Key.StartsWith(prefix))
						{
							outputKey = pair2.Key;
							goto found;
						}
					}
				}
				found:
				;

				if (outputKey == null)
				{
					if (pair.Value.Contains("(stbi__jpeg "))
					{
						outputKey = "Jpg";
					}
					else if (pair.Value.Contains("(stbi__zbuf"))
					{
						outputKey = "Zlib";
					}
					else if (pair.Value.Contains("(stbi__png "))
					{
						outputKey = "Png";
					}
					else if (pair.Value.Contains("(stbi__gif "))
					{
						outputKey = "Gif";
					}
					else if (pair.Value.Contains("(stbi__hdr "))
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

				output[outputKey] += pair.Value;
			}
		}

		private static string PostProcess(string data)
		{
			data = Utility.ReplaceNativeCalls(data);

			data = data.Replace("(int)(a <= 2147483647 - b)", "(a <= 2147483647 - b)?1:0");
			data = data.Replace("(int)(a <= 2147483647 / b)", "(a <= 2147483647 / b)?1:0");
			data = data.Replace("(ulong)((ulong)(w) * bytes_per_pixel)", "(ulong)(w * bytes_per_pixel)");
			data = data.Replace("bytes + row * bytes_per_row", "bytes + (ulong)row * bytes_per_row");
			data = data.Replace("bytes + (h - row - 1) * bytes_per_row", "bytes + (ulong)(h - row - 1) * bytes_per_row");
			data = data.Replace("(void *)(0)", "null");
			data = data.Replace("s.img_buffer_end = s.buffer_start + 1;",
				"s.img_buffer_end = s.buffer_start; s.img_buffer_end++;");
			data = data.Replace("s.img_buffer_end = s.buffer_start + n;",
				"s.img_buffer_end = s.buffer_start; s.img_buffer_end += n;");
			data = data.Replace(" != 0?(null):(null)", " != 0?((byte *)null):(null)");
			data = data.Replace("(int)(j.code_buffer)", "j.code_buffer");
			data = data.Replace("int sixteen = (int)(p != 0);", "int sixteen = (p != 0)?1:0;");
			data = data.Replace("coeff = 0", "coeff = null");
			data =
				data.Replace("if (stbi__zbuild_huffman(&a->z_length, stbi__zdefault_length, (int)(288))== 0) return (int)(0);",
					"fixed (byte* b = stbi__zdefault_length) {if (stbi__zbuild_huffman(&a->z_length, b, (int) (288)) == 0) return (int) (0);}");
			data =
				data.Replace("if (stbi__zbuild_huffman(&a->z_distance, stbi__zdefault_distance, (int)(32))== 0) return (int)(0);",
					"fixed (byte* b = stbi__zdefault_distance) {if (stbi__zbuild_huffman(&a->z_distance, b, (int) (32)) == 0) return (int) (0);}");
			data = data.Replace("sbyte* invalid_chunk = \"XXXX",
				"string invalid_chunk = c.type + \"");

			data = data.Replace("sizeof((s.buffer_start))", "128");
			data = data.Replace("sizeof((temp))", "2048");
			data = data.Replace("sizeof((data[0]))", "sizeof(short)");
			data = data.Replace("sizeof((sizes))", "sizeof(int)");
			data = data.Replace("CRuntime.memset(h.fast, (int)(255), (ulong)(1 << 9));",
				"CRuntime.SetArray(h.fast, (byte)255);");
			data = data.Replace("memset(z->fast, (int)(0), (ulong)(sizeof((z->fast))));",
				"memset(((ushort*)(z->fast)), (int)(0), (ulong)((1 << 9) * sizeof(ushort)));");
			data = data.Replace("memset(codelength_sizes, (int)(0), (ulong)(sizeof((codelength_sizes))));",
				"memset(((byte*)(codelength_sizes)), (int)(0), (ulong)(19 * sizeof(byte)));");
			data = data.Replace("comp != 0", "comp != null");
			data = data.Replace("(int)((tga_image_type) == (3))", "(tga_image_type) == (3)?1:0");

			data = data.Replace("byte* v;", "byte[] v;");

			data = data.Replace("mul_table[bits]", "(int)mul_table[bits]");
			data = data.Replace("shift_table[bits]", "(int)shift_table[bits]");

			data = data.Replace("short* d = ((short*)data.Pointer);",
				"short* d = data;");
			data = data.Replace("byte** coutput = stackalloc byte[4];",
				"byte** coutput = stackalloc byte *[4];");
			data = data.Replace("stbi__resample res_comp = new stbi__resample[4];",
				"var res_comp = new stbi__resample[4]; for (var kkk = 0; kkk < res_comp.Length; ++kkk) res_comp[kkk] = new stbi__resample();");
			data = data.Replace("((byte**)coutput.Pointer)",
				"coutput");
			data = data.Replace("stbi__jpeg j = (stbi__jpeg)(stbi__malloc((ulong)(sizeof(stbi__jpeg))));",
				"stbi__jpeg j = new stbi__jpeg();");
			/*data = data.Replace("stbi__jpeg j = (stbi__jpeg)((stbi__malloc((ulong)(sizeof(stbi__jpeg))))));",
				"stbi__jpeg j = new stbi__jpeg();");
			data = data.Replace("stbi__jpeg j = (stbi__jpeg)((stbi__malloc((ulong)(.Size))));",
				"stbi__jpeg j = new stbi__jpeg();");*/
			data = data.Replace("CRuntime.free(j);",
				string.Empty);
			data = data.Replace("z.img_comp[i].data = (byte*)(((ulong)(z.img_comp[i].raw_data) + 15) & ~15);",
				"z.img_comp[i].data = (byte*)((((long)z.img_comp[i].raw_data + 15) & ~15));");
			data = data.Replace("z.img_comp[i].coeff = (short*)(((ulong)(z.img_comp[i].raw_coeff) + 15) & ~15);",
				"z.img_comp[i].coeff = (short*)((((long)z.img_comp[i].raw_coeff + 15) & ~15));");
			data = data.Replace("(int)(!is_iphone)",
				"is_iphone!=0?0:1");
			data = data.Replace("return (int)(stbi__err(((sbyte*)invalid_chunk.Pointer)));",
				"return (int)(stbi__err(invalid_chunk));");
			data = data.Replace("if ((p) == ((void *)(0))) return (int)(stbi__err(\"outofmem\"));",
				"if (p == null) return (int) (stbi__err(\"outofmem\"));");
			data = data.Replace("pal[color][", "pal[color * 4 +");
			data = data.Replace("pal[i][", "pal[i * 4 +");
			data = data.Replace("pal[v][", "pal[v * 4 +");
			data = data.Replace("pal[g.transparent][", "pal[g.transparent * 4 +");
			data = data.Replace("pal[g.bgindex][", "pal[g.bgindex * 4 +");
			data = data.Replace("uint v = (uint)((uint)((bpp) == (16)?stbi__get16le(s):stbi__get32le(s)));",
				"uint v = (uint)((uint)((bpp) == (16)?(uint)stbi__get16le(s):stbi__get32le(s)));");
			data = data.Replace("(int)(((tga_image_type) == (3)) || ((tga_image_type) == (11)))",
				"(((tga_image_type) == (3))) || (((tga_image_type) == (11)))?1:0");
			data = data.Replace("int r = (int)((stbi__get32be(s)) == (0x38425053));",
				"int r = (((stbi__get32be(s)) == (0x38425053)))?1:0;");
			data = data.Replace("(packets).Size / (packets[0]).Size",
				"packets.Size");
			data = data.Replace("stbi__gif g = (stbi__gif)(stbi__malloc((ulong)(sizeof(stbi__gif))));",
				"stbi__gif g = new stbi__gif();");
			data = data.Replace("CRuntime.free(g);",
				string.Empty);
			data = data.Replace("CRuntime.memset(g, (int)(0), (ulong)(sizeof((g))));",
				string.Empty);
			data = data.Replace("if (((g.transparent) >= (0)) && ((g.eflags & 0x01)))",
				"if (((g.transparent) >= (0)) && ((g.eflags & 0x01) != 0))");
			data = data.Replace("&z.huff_dc[z.img_comp[n].hd]",
				"(stbi__huffman*)z.huff_dc + z.img_comp[n].hd");
			data = data.Replace("&z.huff_ac[ha]",
				"(stbi__huffman*)z.huff_ac + ha");
			data = data.Replace("g.codes[init_code]", "((stbi__gif_lzw*) (g.codes))[init_code]");
			data = data.Replace("&g.codes[avail++]", "(stbi__gif_lzw*)g.codes + avail++");
			data = data.Replace("byte* c = g.pal[g.bgindex]", "byte* c = (byte *)g.pal + g.bgindex");
			data = data.Replace("(g._out_) == (0)", "(g._out_) == null");
			data = data.Replace("((byte*)(tc))[k] = ((byte)(stbi__get16be(s) & 255) * stbi__depth_scale_table[z.depth]);",
				"((byte*)(tc))[k] = (byte)((byte)(stbi__get16be(s) & 255) * stbi__depth_scale_table[z.depth]);");
			data = data.Replace("byte** pal = stackalloc byte[256];",
				"byte* pal = stackalloc byte[256 * 4];");
			data = data.Replace("tga_data = (byte*)(stbi__malloc((ulong)(tga_width) * tga_height * tga_comp));",
				"tga_data = (byte*)(stbi__malloc(tga_width * tga_height * tga_comp));");
			data = data.Replace("case 0x3B:return (byte*)(s);",
				"case 0x3B:return null;");
			data = data.Replace("if ((u) == ((byte*)(s))) u = ((byte*)(0));",
				string.Empty);
			data = data.Replace("sgn = (int)(j.code_buffer >> 31);", "sgn = (int)((int)j.code_buffer >> 31);");

			// Replace some pointers with arrays in some methods signatures
			data = data.Replace("stbi__jpeg_decode_block(stbi__jpeg j, short* data, stbi__huffman hdc, stbi__huffman hac, short* fac, int b, ushort* dequant)",
				"stbi__jpeg_decode_block(stbi__jpeg j, short* data, stbi__huffman hdc, stbi__huffman hac, short[] fac, int b, ushort[] dequant)");
			data = data.Replace("stbi__jpeg_decode_block_prog_ac(stbi__jpeg j, short* data, stbi__huffman hac, short* fac)",
				"stbi__jpeg_decode_block_prog_ac(stbi__jpeg j, short* data, stbi__huffman hac, short[] fac)");
			data = data.Replace("stbi__jpeg_dequantize(short* data, ushort* dequant)",
				"stbi__jpeg_dequantize(short* data, ushort[] dequant)");
			data = data.Replace("stbi__build_fast_ac(short* fast_ac, stbi__huffman h)",
				"stbi__build_fast_ac(short[] fast_ac, stbi__huffman h)");

			return data;
		}

		static void Process()
		{
			var parameters = new ConversionParameters
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
				Namespace = "StbImageSharp",
				Class = "StbImage",
				SkipStructs = new[]
				{
						"stbi_io_callbacks",
						"stbi__context",
						"img_comp",
						"stbi__jpeg",
						"stbi__resample",
						"stbi__gif_lzw",
						"stbi__gif"
					},
				SkipGlobalVariables = new[]
				{
						"stbi__g_failure_reason",
						"stbi__vertically_flip_on_load"
					},
				SkipFunctions = new[]
				{
						"stbi__malloc",
						"stbi_image_free",
						"stbi_failure_reason",
						"stbi__err",
						"stbi_is_hdr_from_memory",
						"stbi_is_hdr_from_callbacks",
						"stbi__pnm_isspace",
						"stbi__pnm_skip_whitespace",
						"stbi__pic_is4",
						"stbi__gif_parse_colortable",
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
						"stbi__hdr_test_core"
					},
				Classes = new[]
				{
						"stbi_io_callbacks",
						"stbi__jpeg",
						"stbi__resample",
						"stbi__gif",
						"stbi__context",
						"stbi__huffman",
						"stbi__png"
					},
				GlobalArrays = new[]
				{
						"stbi__bmask",
						"stbi__jbias",
						"stbi__jpeg_dezigzag",
						"stbi__zlength_base",
						"stbi__zlength_extra",
						"stbi__zdist_base",
						"stbi__zdist_extra",
						"first_row_filter",
						"stbi__depth_scale_table",
						"stbi__zdefault_length",
						"stbi__zdefault_distance",
						"length_dezigzag",
						"png_sig"
					}
			};

			var cp = new ClangParser();

			var result = cp.Process(parameters);

			// Post processing
			Logger.Info("Post processing...");

			var outputFiles = new Dictionary<string, string>();
			Write(result.Constants, outputFiles);
			Write(result.GlobalVariables, outputFiles);
			Write(result.Enums, outputFiles);
			Write(result.Structs, outputFiles);
			Write(result.Methods, outputFiles);

			foreach (var pair in outputFiles)
			{
				var data = PostProcess(pair.Value);

				var sb = new StringBuilder();
				sb.AppendLine(string.Format("// Generated by Sichem at {0}", DateTime.Now));
				sb.AppendLine();

				sb.AppendLine("using System;");
				sb.AppendLine("using System.Runtime.InteropServices;");

				sb.AppendLine();

				sb.Append("namespace StbImageSharp\n{\n\t");
				sb.AppendLine("unsafe partial class StbImage\n\t{");

				data = sb.ToString() + data;
				data += "}\n}";

				File.WriteAllText(@"..\..\..\..\..\src\StbImage.Generated." + pair.Key + ".cs", data);
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
			Console.ReadKey();
		}
	}
}