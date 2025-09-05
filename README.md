# StbImageSharp
[![Nuget](https://img.shields.io/nuget/dt/StbImageSharp)](https://www.nuget.org/packages/StbImageSharp/)
![Build & Publish](https://github.com/StbSharp/StbImageSharp/workflows/Build%20&%20Publish/badge.svg)
[![Chat](https://img.shields.io/discord/628186029488340992.svg)](https://discord.gg/ZeHxhCY)

StbImageSharp is C# port of the stb_image.h, which is C library to load images in JPG, PNG, BMP, TGA, PSD, GIF and HDR formats.

It is important to note, that this project is **port**(not **wrapper**). Original C code had been ported to C#. Therefore StbImageSharp doesnt require any native binaries.

The porting hasn't been done by hand, but using [Hebron](https://github.com/rds1983/Hebron), which is the C to C# code converter utility.

# Adding Reference
There are two ways of referencing StbImageSharp in the project:
1. Through nuget: https://www.nuget.org/packages/StbImageSharp/
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

## ImageInfo
ImageInfo class could be used to obtain an image info like this:
```c#
  ImageInfo? info = ImageInfo.FromStream(imageStream);
```
It'll return null if the image type isnt supported, otherwise it'll return the image info(width, height, color components, etc).

## ImageResultFloat
ImageResultFloat class is similar to ImageResult, but stores data as array of float rathen than bytes. It is used to load HDR files.

## Animated Gifs
It is possible to load all frames of an animated gif along with its delays using following code:
```c#
    foreach(AnimatedFrameResult frame in ImageResult.AnimatedGifFramesFromStream(stream))
    {
        // Do something with a frame
    }
```

# Who uses StbImageSharp?
[MonoGame](http://www.monogame.net/)

[TriLib 2.0](https://ricardoreis.net/?p=778)

# Reliability & Performance
There is special app to measure reliability & performance of StbImageSharp in comparison to the original stb_image.h: https://github.com/StbSharp/StbImageSharp/tree/master/tests/StbImageSharp.Testing

It goes through every image file in the specified folder and tries to load it 10 times with StbImageSharp, then 10 times with C++/CLI wrapper over the original stb_image.h(Stb.Native). Then it compares whether the results are byte-wise similar and also calculates loading times. Also it sums up and reports loading times for each method.

Moreover SixLabor ImageSharp 1.0.4 is included in the testing too.

I've used it over following set of images: https://github.com/StbSharp/TestImages

The byte-wise comprarison results are similar for StbImageSharp and Stb.Native.

And performance comparison results are(times are total loading times):
```
3 -- StbImageSharp - bmp: 34 ms, tga: 499 ms, png: 12028 ms, jpg: 2005 ms, psd: 0 ms, Total: 14566 ms
3 -- Stb.Native - bmp: 23 ms, tga: 401 ms, png: 10271 ms, jpg: 1419 ms, psd: 0 ms, Total: 12114 ms
3 -- ImageSharp - bmp: 24 ms, png: 10253 ms, jpg: 1782 ms, Total: 12059 ms
3 -- Total files processed - bmp: 7, tga: 41, png: 568, jpg: 170, psd: 1, Total: 787
3 -- StbImageSharp/Stb.Native matches/processed - 787/787
```

# License
Public Domain

## Support
[Discord](https://discord.gg/ZeHxhCY)

## Building From Source Code
1. Clone this repo.
2. Open StbImageSharp.sln.

## Sponsor
https://www.patreon.com/rds1983

https://boosty.to/rds1983

bitcoin: 3GeKFcv8X1cn8WqH1mr8i7jgPBkQjQuyN1

# Credits
* [stb](https://github.com/nothings/stb)
