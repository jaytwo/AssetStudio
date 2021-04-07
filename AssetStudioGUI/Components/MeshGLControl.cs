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
        private int mdx, mdy;
        private bool lmdown, rmdown;
        private int pgmID, pgmColorID, pgmBlackID;
        private int attributeVertexPosition;
        private int attributeNormalDirection;
        private int uniformMaterialColor;
        private int uniformModelMatrix;
        private int uniformViewMatrix;
        private int uniformProjMatrix;
        private int vao;
        private Vector3[] vertexData;
        private Vector3[] normalData;
        private Vector3[] normal2Data;
        private Matrix4 modelMatrixData;
        private Matrix4 viewMatrixData;
        private Matrix4 projMatrixData;
        private int[] indiceData;
        private Mesh mesh;

        public int wireFrameMode;
        public int shadeMode;
        public int normalMode;
        public bool glControlLoaded;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            ChangeGLSize(Size);
            GL.ClearColor(System.Drawing.Color.CadetBlue);
            pgmID = GL.CreateProgram();
            LoadShader(MeshGLShaders.Vertex, ShaderType.VertexShader, pgmID);
            LoadShader(MeshGLShaders.Fragment, ShaderType.FragmentShader, pgmID);
            GL.LinkProgram(pgmID);

            pgmColorID = GL.CreateProgram();
            LoadShader(MeshGLShaders.Vertex, ShaderType.VertexShader, pgmColorID);
            LoadShader(MeshGLShaders.FragmentColor, ShaderType.FragmentShader, pgmColorID);
            GL.LinkProgram(pgmColorID);

            pgmBlackID = GL.CreateProgram();
            LoadShader(MeshGLShaders.Vertex, ShaderType.VertexShader, pgmBlackID);
            LoadShader(MeshGLShaders.FragmentBlack, ShaderType.FragmentShader, pgmBlackID);
            GL.LinkProgram(pgmBlackID);

            attributeVertexPosition = GL.GetAttribLocation(pgmID, "vertexPosition");
            attributeNormalDirection = GL.GetAttribLocation(pgmID, "normalDirection");
            uniformMaterialColor = GL.GetUniformLocation(pgmID, "materialColor");
            uniformModelMatrix = GL.GetUniformLocation(pgmID, "modelMatrix");
            uniformViewMatrix = GL.GetUniformLocation(pgmID, "viewMatrix");
            uniformProjMatrix = GL.GetUniformLocation(pgmID, "projMatrix");
            glControlLoaded = true;
        }

        private static void LoadShader(string source, ShaderType type, int program)
        {
            var address = GL.CreateShader(type);
            GL.ShaderSource(address, source);
            GL.CompileShader(address);
            GL.AttachShader(program, address);
            GL.DeleteShader(address);
        }

        private static void CreateVBO(Vector3[] data, int address)
        {
            GL.GenBuffers(1, out int vboAddress);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboAddress);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(data.Length * Vector3.SizeInBytes), data, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(address, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(address);
        }

        private static void CreateVBO(Matrix4 data, int address)
        {
            GL.GenBuffers(1, out int vboAddress);
            GL.UniformMatrix4(address, false, ref data);
        }

        private static void CreateEBO(int[] data)
        {
            GL.GenBuffers(1, out int address);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, address);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(data.Length * sizeof(int)), data, BufferUsageHint.StaticDraw);
        }

        public void CreateVAO()
        {
            GL.DeleteVertexArray(vao);
            GL.GenVertexArrays(1, out vao);
            GL.BindVertexArray(vao);
            CreateVBO(vertexData, attributeVertexPosition);

            if (normalMode == 0)
                CreateVBO(normal2Data, attributeNormalDirection);
            else if (normalData != null)
                CreateVBO(normalData, attributeNormalDirection);
            
            CreateVBO(modelMatrixData, uniformModelMatrix);
            CreateVBO(viewMatrixData, uniformViewMatrix);
            CreateVBO(projMatrixData, uniformProjMatrix);
            CreateEBO(indiceData);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
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

        public void PreviewMesh(Mesh m_Mesh)
        {
            mesh = m_Mesh;
            viewMatrixData = Matrix4.CreateRotationY(-(float)Math.PI / 4) * Matrix4.CreateRotationX(-(float)Math.PI / 6);
            
            // Vertices
            int count = 3;
            if (m_Mesh.m_Vertices.Length == m_Mesh.m_VertexCount * 4)
                count = 4;
            
            vertexData = new Vector3[m_Mesh.m_VertexCount];
            
            // Calculate Bounding
            float[] min = new float[3];
            float[] max = new float[3];
            for (int i = 0; i < 3; i++)
            {
                min[i] = m_Mesh.m_Vertices[i];
                max[i] = m_Mesh.m_Vertices[i];
            }
            for (int v = 0; v < m_Mesh.m_VertexCount; v++)
            {
                for (int i = 0; i < 3; i++)
                {
                    min[i] = Math.Min(min[i], m_Mesh.m_Vertices[v * count + i]);
                    max[i] = Math.Max(max[i], m_Mesh.m_Vertices[v * count + i]);
                }
                vertexData[v] = new Vector3(
                    m_Mesh.m_Vertices[v * count],
                    m_Mesh.m_Vertices[v * count + 1],
                    m_Mesh.m_Vertices[v * count + 2]);
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
            indiceData = m_Mesh.m_SubMeshes.SelectMany(m => m.indices).Select(i => (int)i).ToArray();

            // Normals
            if (m_Mesh.m_Normals != null && m_Mesh.m_Normals.Length > 0)
            {
                if (m_Mesh.m_Normals.Length == m_Mesh.m_VertexCount * 3)
                    count = 3;
                else if (m_Mesh.m_Normals.Length == m_Mesh.m_VertexCount * 4)
                    count = 4;
                normalData = new Vector3[m_Mesh.m_VertexCount];
                for (int n = 0; n < m_Mesh.m_VertexCount; n++)
                {
                    normalData[n] = new Vector3(
                        m_Mesh.m_Normals[n * count],
                        m_Mesh.m_Normals[n * count + 1],
                        m_Mesh.m_Normals[n * count + 2]);
                }
            }
            else
                normalData = null;

            // calculate normal by ourself
            normal2Data = new Vector3[m_Mesh.m_VertexCount];
            int[] normalCalculatedCount = new int[m_Mesh.m_VertexCount];
            for (int i = 0; i < m_Mesh.m_VertexCount; i++)
            {
                normal2Data[i] = Vector3.Zero;
                normalCalculatedCount[i] = 0;
            }
            for (int i = 0; i < indiceData.Length; i = i + 3)
            {
                Vector3 dir1 = vertexData[indiceData[i + 1]] - vertexData[indiceData[i]];
                Vector3 dir2 = vertexData[indiceData[i + 2]] - vertexData[indiceData[i]];
                Vector3 normal = Vector3.Cross(dir1, dir2);
                normal.Normalize();
                for (int j = 0; j < 3; j++)
                {
                    normal2Data[indiceData[i + j]] += normal;
                    normalCalculatedCount[indiceData[i + j]]++;
                }
            }
            for (int i = 0; i < m_Mesh.m_VertexCount; i++)
            {
                if (normalCalculatedCount[i] == 0)
                    normal2Data[i] = new Vector3(0, 1, 0);
                else
                    normal2Data[i] /= normalCalculatedCount[i];
            }
            
            Visible = true;
            CreateVAO();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            MakeCurrent();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.BindVertexArray(vao);

            if (wireFrameMode == 0 || wireFrameMode == 2)
                DrawElements(shadeMode == 0 ? pgmID : pgmColorID, PolygonMode.Fill);

            if (wireFrameMode == 1 || wireFrameMode == 2)
            {
                GL.Enable(EnableCap.PolygonOffsetLine);
                GL.PolygonOffset(-1, -1);
                DrawElements(pgmBlackID, PolygonMode.Line);
                GL.Disable(EnableCap.PolygonOffsetLine);
            }

            GL.BindVertexArray(0);
            GL.Flush();
            SwapBuffers();
        }

        private void DrawElements(int program, PolygonMode polygonMode)
        {
            GL.UseProgram(program);
            GL.UniformMatrix4(uniformModelMatrix, false, ref modelMatrixData);
            GL.UniformMatrix4(uniformViewMatrix, false, ref viewMatrixData);
            GL.UniformMatrix4(uniformProjMatrix, false, ref projMatrixData);
            GL.PolygonMode(MaterialFace.FrontAndBack, polygonMode);

            var start = 0;
            foreach (var subMesh in mesh.m_SubMeshes)
            {
                GL.Uniform4(uniformMaterialColor, 1, subMesh.color);
                GL.DrawRangeElements(PrimitiveType.Triangles, start, indiceData.Length, start + subMesh.indices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);
                start += subMesh.indices.Count;
            }
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

            if (e.Button == MouseButtons.Left)
                lmdown = true;
            
            if (e.Button == MouseButtons.Right)
                rmdown = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (lmdown || rmdown)
            {
                float dx = mdx - e.X;
                float dy = mdy - e.Y;
                mdx = e.X;
                mdy = e.Y;
                if (lmdown)
                {
                    dx *= 0.01f;
                    dy *= 0.01f;
                    viewMatrixData *= Matrix4.CreateRotationX(dy);
                    viewMatrixData *= Matrix4.CreateRotationY(dx);
                }
                if (rmdown)
                {
                    dx *= 0.003f;
                    dy *= 0.003f;
                    viewMatrixData *= Matrix4.CreateTranslation(-dx, dy, 0);
                }
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Left)
                lmdown = false;

            if (e.Button == MouseButtons.Right)
                rmdown = false;
        }
    }
}