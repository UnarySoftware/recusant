#if TOOLS

using Godot;
using Godot.Collections;
using System;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginResourcePreviews : EditorResourcePreviewGenerator, IPluginSystem
    {
        bool ISystem.Initialize()
        {
            EditorInterface.Singleton.GetResourcePreviewer().AddPreviewGenerator(this);
            return true;
        }

        void ISystem.Deinitialize()
        {
            EditorInterface.Singleton.GetResourcePreviewer().RemovePreviewGenerator(this);
        }

        public override bool _Handles(string type)
        {
            return type == "Resource";
        }

        public override Texture2D _GenerateFromPath(string path, Vector2I size, Dictionary metadata)
        {
            if (!ResourceLoader.Exists(path))
            {
                return null;
            }

            string typeName = path.GetScriptType();

            Type type = Types.GetTypeOfName(typeName);

            if (type == null)
            {
                return null;
            }

            string iconPath = null;

            while (true)
            {
                if (type == null)
                {
                    break;
                }

                foreach (var attribute in type.GetCustomAttributes(typeof(IconAttribute), false))
                {
                    iconPath = ((IconAttribute)attribute).Path;
                    break;
                }

                if (iconPath != null)
                {
                    break;
                }

                type = type.BaseType;
            }

            if (iconPath == null)
            {
                return null;
            }

            Texture2D texture = ResourceLoader.Load<Texture2D>(iconPath);

            if (texture == null)
            {
                return null;
            }

            if (texture.GetImage().Duplicate() is not Image img)
            {
                return null;
            }

            if (img == null)
            {
                return null;
            }

            img.ClearMipmaps();

            if (img.IsCompressed())
            {
                if (img.Decompress() != Error.Ok)
                {
                    return null;
                }
            }
            else if (img.GetFormat() != Image.Format.Rgb8 && img.GetFormat() != Image.Format.Rgba8)
            {
                img.Convert(Image.Format.Rgba8);
            }

            Vector2 new_size = img.GetSize();
            if (new_size.X > size.X)
            {
                new_size = new Vector2(size.X, new_size.Y * size.X / new_size.X);
            }
            if (new_size.Y > size.Y)
            {
                new_size = new Vector2(new_size.X * size.Y / new_size.Y, size.Y);
            }
            Vector2I new_size_i = new Vector2I((int)new_size.X, (int)new_size.Y).Max(1);
            img.Resize(new_size_i.X, new_size_i.Y, Image.Interpolation.Cubic);

            return ImageTexture.CreateFromImage(img);
        }

        public override Texture2D _Generate(Resource resource, Vector2I size, Dictionary metadata)
        {
            return null;
        }
    }
}

#endif
