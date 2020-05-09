using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia.Helpers;
using Avalonia.Visuals.Media.Imaging;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Immutable Skia bitmap.
    /// </summary>
    internal class ImmutableBitmap : IDrawableBitmapImpl
    {
        private readonly SKImage _image;

        /// <summary>
        /// Create immutable bitmap from given stream.
        /// </summary>
        /// <param name="stream">Stream containing encoded data.</param>
        public ImmutableBitmap(Stream stream)
        {
            using (var skiaStream = new SKManagedStream(stream))
            {
                using (var data = SKData.Create(skiaStream))
                    _image = SKImage.FromEncodedData(data);

                if (_image == null)
                {
                    throw new ArgumentException("Unable to load bitmap from provided data");
                }

                PixelSize = new PixelSize(_image.Width, _image.Height);

                // TODO: Skia doesn't have an API for DPI.
                Dpi = new Vector(96, 96);
            }
        }

        public ImmutableBitmap(ImmutableBitmap src, PixelSize destinationSize, BitmapInterpolationMode interpolationMode)
        {            
            SKImageInfo info = new SKImageInfo(destinationSize.Width, destinationSize.Height, SKColorType.Bgra8888);
            SKImage output = SKImage.Create(info);
            src._image.ScalePixels(output.PeekPixels(), interpolationMode.ToSKFilterQuality());

            _image = output;

            PixelSize = new PixelSize(_image.Width, _image.Height);

            // TODO: Skia doesn't have an API for DPI.
            Dpi = new Vector(96, 96);
        }

        // NOTE, putting the stream before options in the parameters, causes an exception
        // inside SKCodec.Create with optimized code. Probably a bug in .net compiler.
        // Other option is to have the argument order as desired and use PreserveSig options.
        [MethodImpl(MethodImplOptions.PreserveSig)]
        public ImmutableBitmap(Stream stream, int decodeSize, bool horizontal, BitmapInterpolationMode interpolationMode)
        {
            // create the codec
            var codec = SKCodec.Create(stream);
            var info = codec.Info;

            // get the scale that is nearest to what we want (eg: jpg returned 512)
            var supportedScale = codec.GetScaledDimensions(horizontal ? ((float)decodeSize / info.Width) : ((float)decodeSize / info.Height));

            // decode the bitmap at the nearest size
            var nearest = new SKImageInfo(supportedScale.Width, supportedScale.Height);
            var bmp = SKBitmap.Decode(codec, nearest);

            // now scale that to the size that we want
            var realScale = horizontal ? ((double)info.Height / info.Width) : ((double)info.Width / info.Height);

            SKImageInfo desired;


            if (horizontal)
            {
                desired = new SKImageInfo(decodeSize, (int)(realScale * decodeSize));
            }
            else
            {
                desired = new SKImageInfo((int)(realScale * decodeSize), decodeSize);
            }

            if (bmp.Width != desired.Width || bmp.Height != desired.Height)
            {
                if (bmp.Height != bmp.Width)
                {

                }
                var scaledBmp = bmp.Resize(desired, interpolationMode.ToSKFilterQuality());
                bmp.Dispose();
                bmp = scaledBmp;
            }

            _image = SKImage.FromBitmap(bmp);
            bmp.Dispose();

            if (_image == null)
            {
                throw new ArgumentException("Unable to load bitmap from provided data");
            }

            PixelSize = new PixelSize(_image.Width, _image.Height);

            // TODO: Skia doesn't have an API for DPI.
            Dpi = new Vector(96, 96);
        }

        /// <summary>
        /// Create immutable bitmap from given pixel data copy.
        /// </summary>
        /// <param name="size">Size of the bitmap.</param>
        /// <param name="dpi">DPI of the bitmap.</param>
        /// <param name="stride">Stride of data pixels.</param>
        /// <param name="format">Format of data pixels.</param>
        /// <param name="data">Data pixels.</param>
        public ImmutableBitmap(PixelSize size, Vector dpi, int stride, PixelFormat format, IntPtr data)
        {
            var imageInfo = new SKImageInfo(size.Width, size.Height, format.ToSkColorType(), SKAlphaType.Premul);

            _image = SKImage.FromPixelCopy(imageInfo, data, stride);

            if (_image == null)
            {
                throw new ArgumentException("Unable to create bitmap from provided data");
            }

            PixelSize = size;
            Dpi = dpi;
        }

        public Vector Dpi { get; }
        public PixelSize PixelSize { get; }

        public int Version { get; } = 1;

        /// <inheritdoc />
        public void Dispose()
        {
            _image.Dispose();
        }

        /// <inheritdoc />
        public void Save(string fileName)
        {
            ImageSavingHelper.SaveImage(_image, fileName);
        }

        /// <inheritdoc />
        public void Save(Stream stream)
        {
            ImageSavingHelper.SaveImage(_image, stream);
        }

        /// <inheritdoc />
        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            context.Canvas.DrawImage(_image, sourceRect, destRect, paint);
        }
    }
}
