using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace PSG
{
    /* The Mask class defines a 2D template form which sprites can be generated */
    public class Mask
    {

        public int[,] data;
        public int width;
        public int height;
        public bool mirrorX;
        public bool mirrorY;

        /*   @param {data} Integer array describing which parts of the sprite should be
         *   empty, body, and border. The mask only defines a semi-ridgid stucture
         *   which might not strictly be followed based on randomly generated numbers.
         *
         *      -1 = Always border (black)
         *       0 = Empty
         *       1 = Randomly chosen Empty/Body
         *       2 = Randomly chosen Border/Body
         *
         *   @param {width} Width of the mask data array
         *   @param {height} Height of the mask data array
         *   @param {mirrorX} A boolean describing whether the mask should be mirrored on the x axis
         *   @param {mirrorY} A boolean describing whether the mask should be mirrored on the y axis
         */
        private Mask (int[,] data, bool mirrorX, bool mirrorY)
        {
            this.width = data.GetLength (1);
            this.height = data.GetLength (0);
            this.data = data;
            this.mirrorX = mirrorX;
            this.mirrorY = mirrorY;
        }

        public static Mask FromArray (int[,] data, bool mirrorX = false, bool mirrorY = false)
        {
            return new Mask (data, mirrorX, mirrorY);
        }

        public static Mask FromFile (string fileName, bool mirrorX = false, bool mirrorY = false)
        {
            FileStream fileStream = null;
            Mask mask = null;
            try {
                fileStream = File.OpenRead (fileName);
                using (StreamReader reader = new StreamReader (fileStream)) {
                    fileStream = null;
                    List<int[]> tmpData = new List<int[]> ();
                    while (!reader.EndOfStream) {
                        string line = reader.ReadLine ();
                        string[] stringValues = line.Split (',');
                        int[] values = new int[stringValues.Length];
                        for (int i = 0; i < stringValues.Length; i++) {
                            values [i] = int.Parse (stringValues [i]);
                        }
                        tmpData.Add (values);
                    }
                    int[,] tmpDataArray = new int[tmpData.Count, tmpData [0].Length];
                    for (int i = 0; i < tmpDataArray.GetLength (0); i++) {
                        for (int j = 0; j < tmpDataArray.GetLength (1); j++) {
                            tmpDataArray [i, j] = tmpData [i] [j];
                        }
                    }
                    mask = new Mask (tmpDataArray, mirrorX, mirrorY);
                }
            } finally {
                if (fileStream != null) {
                    fileStream.Dispose ();
                }
            }
            return mask;
        }
    }

    /* Used to generate sprites based on a template and aesthetic parameters */
    public class PixelSpriteGenerator
    {
        private Mask Mask { get; set; }

        private bool IsColored { get; set; }

        private float EdgeBrightness { get; set; }

        private float ColorVariations { get; set; }

        private float BrightnessNoise { get; set; }

        private float Saturation { get; set; }

        private int Scale { get; set; }

        private Color BackgroundColor { get; set; }

        private Color ForegroundColor { get; set; }

        /*
         *   @param {mask} 2D template used to generate the sprite
         *   @param {isColored} Should we add color to the sprite?
         *   @param {edgeBrightness} 1 for heavy outlines, 0 for an undefined blob of color
         *   @param {colorVariations} How often does color change
         *   @param {brightnessNoise} How much color fades on edges of the sprite
         *   @param {saturation} How saturated is the color of the sprite
         *   @param {scale} Changes the size of the output texture for the sprite
         *   @Param {backgroundColor} Color of 0s in the mask
         *   @Param {foregroundColor} Sets fill color to a specific color
         */
        public PixelSpriteGenerator (Mask mask,
                                     bool isColored = true,
                                     float edgeBrightness = 0.3f,
                                     float colorVariations = 0.2f,
                                     float brightnessNoise = 0.3f,
                                     float saturation = 0.5f,
                                     int scale = 1,
                                     Color backgroundColor = default(Color),
                                     Color foregroundColor = default(Color))
        {
            this.Mask = mask;
            this.IsColored = isColored;
            this.EdgeBrightness = edgeBrightness;
            this.ColorVariations = colorVariations;
            this.BrightnessNoise = brightnessNoise;
            this.Saturation = saturation;
            this.Scale = scale;
            this.BackgroundColor = backgroundColor;
            this.ForegroundColor = foregroundColor;
        }

        /*
         *   Creates sprite based on mask and aesthetic parameters
         */
        public Sprite CreateSprite ()
        {
            // double width and height if mirroring
            int width = Mask.width * (Mask.mirrorX ? 2 : 1);
            int height = Mask.height * (Mask.mirrorY ? 2 : 1);

            // initialize data array
            int[,] data = new int[width, height];

            applyMask (data, Mask);
            generateRandomSample (data, width, height);
            if (Mask.mirrorX) {
                mirrorX (data, width, height);
            }
            if (Mask.mirrorY) {
                mirrorY (data, width, height);
            }
            generateEdges (data, width, height);
            return renderToSprite (data, width, height);
        }

        /*
         *   Copies the mask data into the template data array at location (0, 0).
         *   The mask may be smaller than the template data array
         */
        private void applyMask (int[,] data, Mask mask)
        {
            for (int y = 0; y < mask.height; y++) {
                for (int x = 0; x < mask.width; x++) {
                    data [x, y] = mask.data [y, x];
                }
            }
        }

        /*
         *   Mirrors the template data horizontally.
         */
        private void mirrorX (int[,] data, int w, int h)
        {
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w / 2; x++) {
                    data [w - x - 1, y] = data [x, y];
                }
            }
        }

        /*
         *   Mirrors the template data vertically.
         */
        private void mirrorY (int[,] data, int w, int h)
        {
            for (int y = 0; y < h / 2; y++) {
                for (int x = 0; x < w; x++) {
                    data [x, h - y - 1] = data [x, y];
                }
            }
        }

        /*
         *   Apply a random sample to the sprite template.
         *
         *   If the template contains a 1 (internal body part) at location (x, y), then
         *   there is a 50% chance it will be turned empty. If there is a 2, then there
         *   is a 50% chance it will be turned into a body or border.
         *
         *   (feel free to play with this logic for interesting results)
         */
        private void generateRandomSample (int[,] data, int w, int h)
        {
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    int val = data [x, y];

                    if (val == 1) {
                        val = val * Mathf.RoundToInt (Random.value);
                    } else if (val == 2) {
                        if (Random.value > 0.5f) {
                            val = 1;
                        } else {
                            val = -1;
                        }
                    } 

                    data [x, y] = val;
                }
            }
        }

        /*
         *   Applies edges to any template location that is positive in
         *   value and is surrounded by empty (0) pixels.
         */
        private void generateEdges (int[,] data, int w, int h)
        {
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w; x++) {
                    if (data [x, y] > 0) {
                        if (y - 1 >= 0 && data [x, y - 1] == 0) {
                            data [x, y - 1] = -1;
                        }
                        if (y + 1 < h && data [x, y + 1] == 0) {
                            data [x, y + 1] = -1;
                        }
                        if (x - 1 >= 0 && data [x - 1, y] == 0) {
                            data [x - 1, y] = -1;
                        }
                        if (x + 1 < w && data [x + 1, y] == 0) {
                            data [x + 1, y] = -1;
                        }
                    }
                }
            }
        }

        /*
         *   Renders a Sprite based on template data
         */
        private Sprite renderToSprite (int[,] data, int w, int h)
        {
            Texture2D texture = new Texture2D (w * Scale, h * Scale, TextureFormat.ARGB32, false);

            bool isVerticalGradient = Random.value > 0.5f;
            Saturation = Mathf.Max (Mathf.Min (Random.value * Saturation, 1), 0);
            float hue = Random.value;

            int u, v, ulen, vlen;
            if (isVerticalGradient) {
                vlen = h;
                ulen = w;
            } else {
                vlen = w;
                ulen = h;
            }

            for (u = 0; u < ulen; u++) {
                // Create a non-uniform random number between 0 and 1 (lower numbers more likely)
                float isNewColor = Mathf.Abs (((Random.value * 2f - 1f)
                                   + (Random.value * 2f - 1f)
                                   + (Random.value * 2f - 1f)) / 3f);

                // Only change the color sometimes (values above 0.8 are less likely than others)
                if (isNewColor > (1 - ColorVariations)) {
                    hue = Random.value;
                }

                for (v = 0; v < vlen; v++) {
                    int val = isVerticalGradient ? data [u, v] : data [v, u];
                    Color rgb = BackgroundColor;

                    if (val != 0) {
                        if (IsColored) {
                            float brightness = Mathf.Sin ((((float)u) / ((float)ulen)) * Mathf.PI) * (1 - BrightnessNoise) + Random.value * BrightnessNoise;
                            // Get the RGB color value
                            rgb = ForegroundColor != default(Color) ? ForegroundColor : Color.HSVToRGB (hue, Saturation, brightness);

                            // If this is an edge, then darken the pixel
                            if (val == -1) {
                                rgb.r *= EdgeBrightness;
                                rgb.g *= EdgeBrightness;
                                rgb.b *= EdgeBrightness;
                            }

                        } else {
                            // Not colored, simply output black
                            if (val == -1) {
                                rgb = Color.black;
                            }
                        }
                    }
                    for (int i = 0; i < Scale; i++) {
                        for (int j = 0; j < Scale; j++) {
                            int x;
                            int y;
                            if (isVerticalGradient) {
                                x = u * Scale + i;
                                y = v * Scale + j;
                            } else {
                                x = v * Scale + j;
                                y = u * Scale + i;
                            }
                            texture.SetPixel (w * Scale - x, h * Scale - y, rgb);
                        }
                    }
                }
            }
            texture.Apply ();
            return Sprite.Create (texture, new Rect (0, 0, w * Scale, h * Scale), new Vector2 (0.5f, 0.5f));
        }
    }
}