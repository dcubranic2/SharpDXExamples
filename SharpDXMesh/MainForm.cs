using SharpDX;
using SharpDX.Direct3D9;
using SharpDX.Mathematics;
using Color = SharpDX.Color;
using Assimp;
using System.Drawing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace SharpDXMesh
{
    public partial class MainForm : Form
    {
        // Our global variables for this project
        Device device = null; // Our rendering device
        Direct3D Direct3D = null;
        SharpDX.Direct3D9.Font font = null;
        SharpDX.Direct3D9.VertexBuffer vertexBuffer = null;
        SharpDX.Direct3D9.VertexBuffer textureVertexBuffer = null;
        VertexDeclaration textureVertexDecl = null;
        VertexDeclaration vertexDecl = null;
        IndexBuffer indexBuffer = null;
        Texture texture = null;
       
        float angle = 0;
        int fps = 0;
        int fps_next = 0;
        int tick_count = 0;
        bool wireframe = false;
        bool lightning = true;
        [StructLayout(LayoutKind.Sequential)]
        struct Vertex
        {
            public Vector4 Position; // from 0 to 15 bytes
            public SharpDX.ColorBGRA Color; // from 16 to 19 bytes
            public Vector3 Normal; // from 20 to 31 bytes
        }
        [StructLayout(LayoutKind.Sequential)]
        struct TextureVertex
        {
            public Vector3 Position;
            public float U;
            public float V;
        }
        public MainForm()
        {
            // Set the initial size of our form
            this.ClientSize = new System.Drawing.Size(800, 600);
            // And it's caption
            this.Text = "D3D Tutorial 03: Working with Meshes";
            // We are having control of window painting and window is not transparent
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
            // Load our icon from the resources of the .exe
            this.Icon = new Icon("assets//directx.ico");
            InitializeGraphics();
        }

        public bool InitializeGraphics()
        {
            try
            {
                // Now let's setup our D3D stuffcard;
                Direct3D = new Direct3D();

                // Now let's setup our D3D stuff
                PresentParameters presentParams = new PresentParameters();
                presentParams.Windowed = true;
                presentParams.SwapEffect = SwapEffect.Discard;

                device = new Device(Direct3D, 0, DeviceType.Hardware, this.Handle, CreateFlags.SoftwareVertexProcessing, presentParams);

                //set up camera --> view transform
                device.SetTransform(TransformState.View, Matrix.LookAtLH(new Vector3(0, 0f, 300), new Vector3(), new Vector3(0, 1, 0)));
                //set up --> projection matrix
                device.SetTransform(TransformState.Projection, Matrix.PerspectiveFovLH(((float)System.Math.PI) / 4, this.Width / this.Height, 0.1f, 600f));

                // Initialize the Font
                FontDescription fontDescription = new FontDescription()
                {
                    Height = 14,
                    Italic = false,
                    CharacterSet = FontCharacterSet.Ansi,
                    FaceName = "Arial",
                    MipLevels = 0,
                    OutputPrecision = FontPrecision.TrueType,
                    PitchAndFamily = FontPitchAndFamily.Default,
                    Quality = FontQuality.ClearType,
                    Weight = FontWeight.Bold
                };

                font = new SharpDX.Direct3D9.Font(device, fontDescription);

                tick_count = System.Environment.TickCount;

                Assimp.AssimpContext assimpContext = new Assimp.AssimpContext();
                Assimp.Configs.NormalSmoothingAngleConfig normalSmoothingAngle = new Assimp.Configs.NormalSmoothingAngleConfig(66.0f);
                assimpContext.SetConfig(normalSmoothingAngle);

                Assimp.Scene mesh = assimpContext.ImportFile(System.IO.Path.Combine(System.Environment.CurrentDirectory,"assets\\tiger.x"));

                //create vertex buffer
                textureVertexBuffer = new SharpDX.Direct3D9.VertexBuffer(device, Utilities.SizeOf<TextureVertex>() * mesh.Meshes[0].VertexCount, Usage.WriteOnly,VertexFormat.None, Pool.Default);
                DataStream xds1 = textureVertexBuffer.Lock(0, 0, LockFlags.None);
                for (int i = 0; i < mesh.Meshes[0].VertexCount; i++)
                    xds1.Write<TextureVertex>(new TextureVertex()
                    {
                        Position = new Vector3(mesh.Meshes[0].Vertices[i].X, mesh.Meshes[0].Vertices[i].Y, mesh.Meshes[0].Vertices[i].Z),
                        U=mesh.Meshes[0].TextureCoordinateChannels[0][i].X,
                        V=mesh.Meshes[0].TextureCoordinateChannels[0][i].Y
                    });
                textureVertexBuffer.Unlock();
                var vertexElems1 = new[] {
                    new VertexElement(1, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
                    new VertexElement(1, 12, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                    VertexElement.VertexDeclarationEnd
                };

                textureVertexDecl = new VertexDeclaration(device, vertexElems1);
                //create index buffer

                //retrive materials
                System.Drawing.Image bmp = Image.FromFile(System.IO.Path.Combine(System.Environment.CurrentDirectory, "assets\\" + mesh.Materials[0].TextureDiffuse.FilePath));
                IntPtr pval = IntPtr.Zero;
                System.Drawing.Imaging.BitmapData bd = ((Bitmap)bmp).LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);
                try
                {
                    pval = bd.Scan0;
                    Format format = Format.X8R8G8B8;// Format.P8;
                    DataStream textStream = null;
                    if (Helper.Utils.IsTextureFormatOk(device, format, Direct3D.GetAdapterDisplayMode(0).Format))
                    {
                        texture = new Texture(device, bmp.Width, bmp.Height, 1, Usage.None, format, Pool.Managed);
                        texture.LockRectangle(0, LockFlags.None, out textStream);
                        //textStream.WriteRange(pval, bmp.Width * bmp.Height);
                        for (int i = 0; i < bmp.Width * bmp.Height; i++)
                        {
                            textStream.WriteByte(0x00); //B
                            textStream.WriteByte(0x00); //G
                            textStream.WriteByte(0xff); //R
                            textStream.WriteByte(0x00); //X
                        }
                        texture.UnlockRectangle(0);
                    }
                        
                }
                finally
                {
                    ((Bitmap)bmp).UnlockBits(bd);
                }

                assimpContext.Dispose();


                vertexBuffer = new VertexBuffer(device, Utilities.SizeOf<Vertex>() * 36, Usage.WriteOnly, VertexFormat.None, Pool.Managed);
                DataStream xds = vertexBuffer.Lock(0, Utilities.SizeOf<Vertex>() * 36, LockFlags.None);
                xds.WriteRange<Vertex>(new Vertex[] {
				//left
				new Vertex() { Color = Color.Red, Position = new Vector4( -50f ,  50f,  50f, 1.0f) , Normal = new Vector3(-1,0,0)  },
                new Vertex() { Color = Color.Red, Position = new Vector4( -50f , -50f, -50f, 1.0f) , Normal = new Vector3(-1,0,0)  },
                new Vertex() { Color = Color.Red, Position = new Vector4( -50f , -50f,  50f, 1.0f) , Normal = new Vector3(-1,0,0)  },
                new Vertex() { Color = Color.Red, Position = new Vector4( -50f ,  50f, -50f, 1.0f) , Normal = new Vector3(-1,0,0)  },
                new Vertex() { Color = Color.Red, Position = new Vector4( -50f , -50f, -50f, 1.0f) , Normal = new Vector3(-1,0,0)  },
                new Vertex() { Color = Color.Red, Position = new Vector4( -50f ,  50f,  50f, 1.0f) , Normal = new Vector3(-1,0,0)  },

				//right
				new Vertex() { Color = Color.Green, Position = new Vector4( 50f ,  50f,  50f, 1.0f) , Normal = new Vector3(1,0,0)  },
                new Vertex() { Color = Color.Green, Position = new Vector4( 50f , -50f,  50f, 1.0f) , Normal = new Vector3(1,0,0)  },
                new Vertex() { Color = Color.Green, Position = new Vector4( 50f , -50f, -50f, 1.0f) , Normal = new Vector3(1,0,0)  },
                new Vertex() { Color = Color.Green, Position = new Vector4( 50f ,  50f, -50f, 1.0f) , Normal = new Vector3(1,0,0)  },
                new Vertex() { Color = Color.Green, Position = new Vector4( 50f ,  50f,  50f, 1.0f) , Normal = new Vector3(1,0,0)  },
                new Vertex() { Color = Color.Green, Position = new Vector4( 50f , -50f, -50f, 1.0f) , Normal = new Vector3(1,0,0)  },

				//top
				new Vertex() { Color = Color.Blue, Position = new Vector4( -50f , 50f ,  50f, 1.0f) , Normal = new Vector3(0,1,0)  },
                new Vertex() { Color = Color.Blue, Position = new Vector4(  50f , 50f , -50f, 1.0f) , Normal = new Vector3(0,1,0)  },
                new Vertex() { Color = Color.Blue, Position = new Vector4( -50f , 50f , -50f, 1.0f) , Normal = new Vector3(0,1,0)  },
                new Vertex() { Color = Color.Blue, Position = new Vector4( -50f , 50f ,  50f, 1.0f) , Normal = new Vector3(0,1,0)  },
                new Vertex() { Color = Color.Blue, Position = new Vector4(  50f , 50f ,  50f, 1.0f) , Normal = new Vector3(0,1,0)  },
                new Vertex() { Color = Color.Blue, Position = new Vector4(  50f , 50f , -50f, 1.0f) , Normal = new Vector3(0,1,0)  },

				//bottom
				new Vertex() { Color = Color.Yellow, Position = new Vector4( -50f , -50f ,  50f, 1.0f) , Normal = new Vector3(0,-1,0) },
                new Vertex() { Color = Color.Yellow, Position = new Vector4( -50f , -50f , -50f, 1.0f) , Normal = new Vector3(0,-1,0) },
                new Vertex() { Color = Color.Yellow, Position = new Vector4(  50f , -50f , -50f, 1.0f) , Normal = new Vector3(0,-1,0) },
                new Vertex() { Color = Color.Yellow, Position = new Vector4( -50f , -50f ,  50f, 1.0f) , Normal = new Vector3(0,-1,0) },
                new Vertex() { Color = Color.Yellow, Position = new Vector4(  50f , -50f , -50f, 1.0f) , Normal = new Vector3(0,-1,0) },
                new Vertex() { Color = Color.Yellow, Position = new Vector4(  50f , -50f ,  50f, 1.0f) , Normal = new Vector3(0,-1,0) },

				//front
				new Vertex() { Color = Color.Silver, Position = new Vector4(  -50f ,  50f, 50f , 1.0f) , Normal = new Vector3(0,0,1) },
                new Vertex() { Color = Color.Silver, Position = new Vector4(  -50f , -50f, 50f , 1.0f) , Normal = new Vector3(0,0,1) },
                new Vertex() { Color = Color.Silver, Position = new Vector4(   50f ,  50f, 50f , 1.0f) , Normal = new Vector3(0,0,1) },
                new Vertex() { Color = Color.Silver, Position = new Vector4(  -50f , -50f, 50f , 1.0f) , Normal = new Vector3(0,0,1) },
                new Vertex() { Color = Color.Silver, Position = new Vector4(   50f , -50f, 50f , 1.0f) , Normal = new Vector3(0,0,1) },
                new Vertex() { Color = Color.Silver, Position = new Vector4(   50f ,  50f, 50f , 1.0f) , Normal = new Vector3(0,0,1) },

				//back
				new Vertex() { Color = Color.Maroon, Position = new Vector4(  -50f ,  50f, -50f , 1.0f) , Normal = new Vector3(0,0,-1)  },
                new Vertex() { Color = Color.Maroon, Position = new Vector4(   50f ,  50f, -50f , 1.0f) , Normal = new Vector3(0,0,-1)  },
                new Vertex() { Color = Color.Maroon, Position = new Vector4(  -50f , -50f, -50f , 1.0f) , Normal = new Vector3(0,0,-1)  },
                new Vertex() { Color = Color.Maroon, Position = new Vector4(  -50f , -50f, -50f , 1.0f) , Normal = new Vector3(0,0,-1)  },
                new Vertex() { Color = Color.Maroon, Position = new Vector4(   50f ,  50f, -50f , 1.0f) , Normal = new Vector3(0,0,-1)  },
                new Vertex() { Color = Color.Maroon, Position = new Vector4(   50f , -50f, -50f , 1.0f) , Normal = new Vector3(0,0,-1)  },

                });
                vertexBuffer.Unlock();

                var vertexElems = new[] {
                    new VertexElement(0, 0, DeclarationType.Float4, DeclarationMethod.Default, DeclarationUsage.Position, 0),
                    new VertexElement(0, 16, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color, 0),
                    new VertexElement(0, 20, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Normal, 0),
                    VertexElement.VertexDeclarationEnd
                };

                vertexDecl = new VertexDeclaration(device, vertexElems);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private void Render()
        {
            if (device == null)
                return;

            //D3DFILL_POINT = 1,
            //D3DFILL_WIREFRAME = 2,
            //D3DFILL_SOLID = 3,
            if (wireframe)
            {
                device.SetRenderState<RenderState>(RenderState.FillMode, (RenderState)2);
            }
            else
            {
                device.SetRenderState<RenderState>(RenderState.FillMode, (RenderState)3);
            }

            if (lightning)
            {
                device.SetRenderState(RenderState.Lighting,true);
            }
            else
            {
                device.SetRenderState(RenderState.Lighting, false);
            }

            if (System.Environment.TickCount - tick_count > 1000)
            {
                fps_next = fps;
                fps = 0;
                tick_count = System.Environment.TickCount;
            }

            fps++;

            angle += 0.01f;

            //Clear the backbuffer to a blue color 
            device.Clear(ClearFlags.Target, SharpDX.Color.White, 1.0f, 0);

            //set up --> rotation matrix
            device.SetTransform(TransformState.World, Matrix.RotationYawPitchRoll(angle / (float)Math.PI, angle / (float)Math.PI * 2, angle / (float)Math.PI));
            device.SetRenderState(RenderState.CullMode, Cull.Counterclockwise);

            //setup light
            SharpDX.Direct3D9.Light xlight = new SharpDX.Direct3D9.Light();
            xlight.Type = LightType.Point;
            xlight.Position = new Vector3(0, 0, 300);
            xlight.Diffuse = Color.White;
            xlight.Range = 10000f;
            xlight.Attenuation0 = 0.2f;
            device.SetLight(0, ref xlight);
            device.EnableLight(0, true);

            //Begin the scene
            device.BeginScene();

            // Rendering of scene objects can happen here
            device.SetStreamSource(0, vertexBuffer, 0 , Utilities.SizeOf<Vertex>());
            device.VertexDeclaration = vertexDecl;
            device.DrawPrimitives(SharpDX.Direct3D9.PrimitiveType.TriangleList, 0, 12);

            device.SetTransform(TransformState.World, Matrix.Scaling(70f, 70f, 70f)* Matrix.RotationYawPitchRoll(angle / (float)Math.PI, angle / (float)Math.PI * 2, angle / (float)Math.PI));
            device.SetStreamSource(1, textureVertexBuffer, 0, Utilities.SizeOf<TextureVertex>());
            device.VertexDeclaration = textureVertexDecl;
            device.SetTexture(0, texture);
            device.DrawPrimitives(SharpDX.Direct3D9.PrimitiveType.TriangleList, 0, 600);

            font.DrawText(null, (fps_next).ToString(), 20, 20, Color.Red);

            //End the scene
            device.EndScene();
            device.Present();
        }
        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            this.Render(); // Render on painting
            this.Invalidate();
        }
        protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
        {
            if ((int)(byte)e.KeyChar == (int)System.Windows.Forms.Keys.Escape)
                this.Close(); // Esc was pressed

            //toggle buttons
            if (e.KeyChar == 'W' || e.KeyChar == 'w')
            {
                wireframe = !wireframe;
            }

            if (e.KeyChar == 'L' || e.KeyChar == 'l')
            {
                lightning = !lightning;
            }
        }
    }
}
