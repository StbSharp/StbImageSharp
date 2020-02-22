# StbImageSharp
[![NuGet](https://img.shields.io/nuget/v/StbImageSharp.svg)](https://www.nuget.org/packages/StbImageSharp/) [![Build status](https://ci.appveyor.com/api/projects/status/c9eh0e4c70ki26fy?svg=true)](https://ci.appveyor.com/project/RomanShapiro/stbimagesharp) [![Chat](https://img.shields.io/discord/628186029488340992.svg)](https://discord.gg/ZeHxhCY)

StbImageSharp is C# port of the stb_image.h, which is C library to load images in JPG, PNG, BMP, TGA, PSD and GIF formats.

It is important to note, that this project is **port**(not **wrapper**). Original C code had been ported to C#. Therefore StbImageSharp doesnt require any native binaries.

The porting hasn't been done by hand, but using [Sichem](https://github.com/rds1983/Sichem), which is the C to C# code converter utility.

# Adding Reference
There are two ways of referencing StbImageSharp in the project:
1. Through nuget: `install-package StbImageSharp`
2. As submodule:
    
    a. `git submodule add https://github.com/StbSharp/StbImageSharp.git`
    
    b. Now there are two options:
       
      * Add src/StbImageSharp.csproj to the solution
       
      * Include *.cs from folder "src" directly in the project. In this case, it might make sense to add STBSHARP_INTERNAL build compilation symbol to the project, so StbImageSharp classes would become internal.
     
# Usage
StbImageSharp exposes API similar to stb_image.h. However that API is complicated and deals with raw unsafe pointers.

Thus several utility classes had been made to wrap that functionality.

'ImageResult.FromStream' loads an image from stream:
```c# 
  using(var stream = File.OpenRead(path))
  {
    ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
  }
```

'ImageResult.FromMemory' loads an image from byte array:
```c# 
  byte[] buffer = File.ReadAllBytes(path);
  ImageResult image = ImageResult.FromMemory(buffer, ColorComponents.RedGreenBlueAlpha);
```

Both code samples will try to load an image (JPG/PNG/BMP/TGA/PSD/GIF) located at 'path'. It'll throw Exception on failure.

If you are writing MonoGame application and would like to convert that data to the Texture2D. It could be done following way:
```c#
Texture2D texture = new Texture2D(GraphicsDevice, image.Width, image.Height, false, SurfaceFormat.Color);
texture.SetData(image.Data);
```

Or if you are writing WinForms app and would like StbSharp resulting bytes to be converted to the Bitmap. The sample code is:
```c#
byte[] data = image.Data;
// Convert rgba to bgra
for (int i = 0; i < x*y; ++i)
{
	byte r = data[i*4];
	byte g = data[i*4 + 1];
	byte b = data[i*4 + 2];
	byte a = data[i*4 + 3];


	data[i*4] = b;
	data[i*4 + 1] = g;
	data[i*4 + 2] = r;
	data[i*4 + 3] = a;
}

// Create Bitmap
Bitmap bmp = new Bitmap(_loadedImage.Width, _loadedImage.Height, PixelFormat.Format32bppArgb);
BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, _loadedImage.Width, _loadedImage.Height), ImageLockMode.WriteOnly,
	bmp.PixelFormat);

Marshal.Copy(data, 0, bmpData.Scan0, bmpData.Stride*bmp.Height);
bmp.UnlockBits(bmpData);
```

# Who uses it?
[MonoGame](http://www.monogame.net/) uses StbImageSharp for Texture2D.FromStream

# Reliability & Performance
There is special app to measure reliability & performance of StbImageSharp in comparison to the original stb_image.h: https://github.com/StbSharp/StbImageSharp/tree/master/tests/StbImageSharp.Testing

It goes through every image file in the specified folder and tries to load it 10 times with StbImageSharp, then 10 times with C++/CLI wrapper over the original stb_image.h(Stb.Native). Then it compares whether the results are byte-wise similar and also calculates loading times. Also it sums up and reports loading times for each method.

Moreover SixLabor ImageSharp is included in the testing too.

I've used it over following set of images: https://github.com/StbSharp/TestImages

The byte-wise comprarison results are similar for StbImageSharp and Stb.Native.

And performance comparison results are(times are total loading times):
```
7 -- StbImageSharp - jpg: 12005 ms, tga: 2055 ms, bmp: 152 ms, psd: 0 ms, png: 65896 ms, Total: 80108 ms
7 -- Stb.Native - jpg: 3827 ms, tga: 1823 ms, bmp: 55 ms, psd: 0 ms, png: 58256 ms, Total: 63961 ms
7 -- ImageSharp - jpg: 99599 ms, bmp: 30 ms, png: 65552 ms, Total: 165181 ms
7 -- Total files processed - jpg: 170, tga: 41, bmp: 7, psd: 1, png: 568, Total: 787
```

# License
Public Domain

# Credits
* [stb](https://github.com/nothings/stb)
