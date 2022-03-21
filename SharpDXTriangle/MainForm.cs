using SharpDX;
using SharpDX.Direct3D9;
using Color = SharpDX.Color;
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

namespace SharpDXTriangle
{
    public partial class MainForm : Form
    {
			// Our global variables for this project
			Device device = null; // Our rendering device
			Direct3D Direct3D = null;

			[StructLayout(LayoutKind.Sequential)]
			struct Vertex
			{
				public Vector4 Position;
				public SharpDX.ColorBGRA Color;
			}
		public MainForm()
			{
				// Set the initial size of our form
				this.ClientSize = new System.Drawing.Size(800, 600);
				// And it's caption
				this.Text = "D3D Tutorial 01: VertexBuffer Triangle";
				// We are having control of window painting and window is not transparent
				this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
				// Load our icon from the resources of the .exe
				//this.Icon = new Icon(this.GetType(), "directx.ico");
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

					device = new Device(Direct3D,0, DeviceType.Hardware, this.Handle, CreateFlags.SoftwareVertexProcessing, presentParams);
					return true;
				}
				catch (Exception)
				{
					return false;
				}
			}
			private void Render()
			{
				if (device == null)
					return;

				//Clear the backbuffer to a blue color 
				device.Clear(ClearFlags.Target, SharpDX.Color.Black, 1.0f, 0);

				SharpDX.Direct3D9.VertexBuffer vertexBuffer = new VertexBuffer(device, Utilities.SizeOf<Vector4>() * 2 * 3, Usage.WriteOnly, VertexFormat.None, Pool.Managed);
				DataStream xds = vertexBuffer.Lock(0, Utilities.SizeOf<Vector4>() * 2 * 3, LockFlags.None);
				xds.WriteRange<Vertex>(new Vertex[] {
				new Vertex() { Color = Color.Red, Position = new Vector4(400.0f, 100.0f, 0.5f, 1.0f) },
				new Vertex() { Color = Color.Blue, Position = new Vector4(650.0f, 500.0f, 0.5f, 1.0f) },
				new Vertex() { Color = Color.Green, Position = new Vector4(150.0f, 500.0f, 0.5f, 1.0f) }
				});
				vertexBuffer.Unlock();

				var vertexElems = new[] {
					new VertexElement(0, 0, DeclarationType.Float4, DeclarationMethod.Default, DeclarationUsage.PositionTransformed, 0),
					new VertexElement(0, 16, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color, 0),
					VertexElement.VertexDeclarationEnd
				};

				var vertexDecl = new VertexDeclaration(device, vertexElems);
				
				//Begin the scene
				device.BeginScene();

				// Rendering of scene objects can happen here
				device.SetStreamSource(0, vertexBuffer, 0, Utilities.SizeOf<Vertex>());
				device.VertexDeclaration = vertexDecl;
				device.DrawPrimitives(PrimitiveType.TriangleList, 0, 1);
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
		}

	}
}
