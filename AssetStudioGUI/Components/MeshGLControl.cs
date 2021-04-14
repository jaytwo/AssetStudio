using AssetStudio;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Vector3 = OpenTK.Vector3;

namespace AssetStudioGUI
{
    class MeshGLControl : GLControl
    {
        private int _vertexBufferObject;
        private int _indexBufferObject;
        private int _vaoModel;
        private MeshGLShader _shader;
        private Mesh _mesh;

        private int mdx, mdy;
        private Matrix4 modelMatrixData;
        private Matrix4 viewMatrixData;
        private Matrix4 projMatrixData;
        private float[] verticeData;
        private uint[] indiceData;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            _shader = new MeshGLShader("Shaders/shader.vert", "Shaders/lighting.frag");
        }

        public void ChangeGLSize(Size size)
        {
            GL.Viewport(0, 0, size.Width, size.Height);

            if (size.Width <= size.Height)
            {
                float k = 1.0f * size.Width / size.Height;
                projMatrixData = Matrix4.CreateScale(1, k, 1);
            }
            else
            {
                float k = 1.0f * size.Height / size.Width;
                projMatrixData = Matrix4.CreateScale(k, 1, 1);
            }
        }

        public void PreviewMesh(Mesh mesh)
        {
            _mesh = mesh;
            viewMatrixData = Matrix4.CreateRotationY(-(float)Math.PI / 4) * Matrix4.CreateRotationX(-(float)Math.PI / 6);

            if (_vaoModel != 0)
            {
                GL.DeleteVertexArray(_vaoModel);
                GL.DeleteBuffer(_indexBufferObject);
                GL.DeleteBuffer(_vertexBufferObject);
            }

            Visible = true;

            // Vertices
            verticeData = new float[mesh.m_Vertices.Length * 2];
            for (int v = 0; v < mesh.m_Vertices.Length; v += 3)
                for (int i = 0; i < 3; i++)
                    verticeData[v * 2 + i] = mesh.m_Vertices[v + i];

            // Calculate Bounding
            float[] min = new float[3];
            float[] max = new float[3];

            for (int i = 0; i < 3; i++)
            {
                min[i] = verticeData[i];
                max[i] = verticeData[i];
            }

            for (int v = 0; v < verticeData.Length; v += 6)
                for (int i = 0; i < 3; i++)
                {
                    min[i] = Math.Min(min[i], verticeData[v + i]);
                    max[i] = Math.Max(max[i], verticeData[v + i]);
                }

            // Calculate modelMatrix
            Vector3 dist = Vector3.One, offset = Vector3.Zero;
            for (int i = 0; i < 3; i++)
            {
                dist[i] = max[i] - min[i];
                offset[i] = (max[i] + min[i]) / 2;
            }
            float d = Math.Max(1e-5f, dist.Length);
            modelMatrixData = Matrix4.CreateTranslation(-offset) * Matrix4.CreateScale(2f / d);

            // Indicies
            indiceData = mesh.m_SubMeshes.SelectMany(m => m.indices).ToArray();

            // calculate normal by ourself
            int[] normalCalculatedCount = new int[verticeData.Length / 6];
            for (int i = 0; i < indiceData.Length; i += 3)
            {
                Vector3 vertex0 = new Vector3(verticeData[indiceData[i + 0] * 6], verticeData[indiceData[i + 0] * 6 + 1], verticeData[indiceData[i + 0] * 6 + 2]);
                Vector3 vertex1 = new Vector3(verticeData[indiceData[i + 1] * 6], verticeData[indiceData[i + 1] * 6 + 1], verticeData[indiceData[i + 1] * 6 + 2]);
                Vector3 vertex2 = new Vector3(verticeData[indiceData[i + 2] * 6], verticeData[indiceData[i + 2] * 6 + 1], verticeData[indiceData[i + 2] * 6 + 2]);
                Vector3 normal = Vector3.Cross(vertex1 - vertex0, vertex2 - vertex0);
                normal.Normalize();
                for (int j = 0; j < 3; j++)
                {
                    verticeData[indiceData[i + j] * 6 + 3] += normal.X;
                    verticeData[indiceData[i + j] * 6 + 4] += normal.Y;
                    verticeData[indiceData[i + j] * 6 + 5] += normal.Z;
                    normalCalculatedCount[indiceData[i + j]]++;
                }
            }
            for (int i = 0; i < normalCalculatedCount.Length; i++)
            {
                if (normalCalculatedCount[i] == 0)
                {
                    verticeData[i * 6 + 3] = 0;
                    verticeData[i * 6 + 4] = 1;
                    verticeData[i * 6 + 5] = 0;
                }
                else
                {
                    verticeData[i * 6 + 3] /= normalCalculatedCount[i];
                    verticeData[i * 6 + 4] /= normalCalculatedCount[i];
                    verticeData[i * 6 + 5] /= normalCalculatedCount[i];
                }
            }

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, verticeData.Length * sizeof(float), verticeData, BufferUsageHint.StaticDraw);

            _indexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indiceData.Length * sizeof(uint), indiceData, BufferUsageHint.StaticDraw);

            _vaoModel = GL.GenVertexArray();
            GL.BindVertexArray(_vaoModel);

            var positionLocation = _shader.GetAttribLocation("aPos");
            GL.EnableVertexAttribArray(positionLocation);
            GL.VertexAttribPointer(positionLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);

            var normalLocation = _shader.GetAttribLocation("aNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));

            ChangeGLSize(Size);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            MakeCurrent();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.BindVertexArray(_vaoModel);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBufferObject);

            _shader.Use();
            _shader.SetMatrix4("model", modelMatrixData);
            _shader.SetMatrix4("view", viewMatrixData);
            _shader.SetMatrix4("projection", projMatrixData);
            _shader.SetVector3("viewPos", new Vector3(0, 0, -1.0f));

            // Here we set the material values of the cube, the material struct is just a container so to access
            // the underlying values we simply type "material.value" to get the location of the uniform
            _shader.SetVector3("material.specular", new Vector3(0.5f, 0.5f, 0.5f));
            _shader.SetFloat("material.shininess", 32.0f);

            // The ambient light is less intensive than the diffuse light in order to make it less dominant
            _shader.SetVector3("light.position", new Vector3(1.2f, 1.0f, -1.2f));
            _shader.SetVector3("light.ambient", new Vector3(0.2f));
            _shader.SetVector3("light.diffuse", new Vector3(0.5f));
            _shader.SetVector3("light.specular", new Vector3(1.0f, 1.0f, 1.0f));

            var start = 0;
            foreach (var subMesh in _mesh.m_SubMeshes)
            {
                _shader.SetVector3("material.ambient", new Vector3(subMesh.color[0], subMesh.color[1], subMesh.color[2]));
                _shader.SetVector3("material.diffuse", new Vector3(subMesh.color[0], subMesh.color[1], subMesh.color[2]));
                GL.DrawElements(PrimitiveType.Triangles, subMesh.indices.Count, DrawElementsType.UnsignedInt, (IntPtr)(start * sizeof(uint)));
                start += subMesh.indices.Count;
            }

            SwapBuffers();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (Visible)
            {
                viewMatrixData *= Matrix4.CreateScale(1 + e.Delta / 1000f);
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            mdx = e.X;
            mdy = e.Y;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                float dx = mdx - e.X;
                float dy = mdy - e.Y;

                mdx = e.X;
                mdy = e.Y;

                if (e.Button == MouseButtons.Left)
                {
                    dx *= 0.01f;
                    dy *= 0.01f;
                    viewMatrixData *= Matrix4.CreateRotationX(dy);
                    viewMatrixData *= Matrix4.CreateRotationY(dx);
                }

                if (e.Button == MouseButtons.Right)
                {
                    dx *= 0.003f;
                    dy *= 0.003f;
                    viewMatrixData *= Matrix4.CreateTranslation(-dx, dy, 0);
                }

                Invalidate();
            }
        }
    }
}