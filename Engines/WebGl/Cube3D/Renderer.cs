using Bridge;
using Bridge.GLMatrix;
using Bridge.Html5;
using Bridge.WebGL;
using System;
using System.Collections.Generic;
using FlatRedBall;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall
{
    public class Renderer
    {
        public List<Sprite> Sprites = new List<Sprite>();
        public HTMLCanvasElement canvas;
        public static WebGLRenderingContext WebGlRenderingContext;
        public WebGLProgram program;


        public bool useBlending = true;
        public double alpha = 1;
        public bool useLighting = true;
        public double ambientR = 0.4;
        public double ambientG = 0.4;
        public double ambientB = 0.4;
        public double lightDirectionX = 0;
        public double lightDirectionY = 0;
        public double lightDirectionZ = -1;
        public double directionalR = 0.25;
        public double directionalG = 0.25;
        public double directionalB = 0.25;

        public double[] mvMatrix = Mat4.Create();
        public double[][] mvMatrixStack = new double[][] { };
        public double[] pMatrix = Mat4.Create();

        public int vertexPositionAttribute;
        public int vertexNormalAttribute;
        public int textureCoordAttribute;

        public WebGLUniformLocation pMatrixUniform;
        public WebGLUniformLocation mvMatrixUniform;
        public WebGLUniformLocation nMatrixUniform;
        public WebGLUniformLocation samplerUniform;
        public WebGLUniformLocation useLightingUniform;
        public WebGLUniformLocation ambientColorUniform;
        public WebGLUniformLocation lightingDirectionUniform;
        public WebGLUniformLocation directionalColorUniform;
        public WebGLUniformLocation alphaUniform;

        public WebGLBuffer cubeVertexPositionBuffer;
        public WebGLBuffer cubeVertexNormalBuffer;
        public WebGLBuffer cubeVertexTextureCoordBuffer;
        public WebGLBuffer cubeVertexIndexBuffer;

        public double xRotation = 0;
        public int xSpeed = 0;

        public double yRotation = 0;
        public int ySpeed = 4;

        public Dictionary<int, bool> currentlyPressedKeys = new Dictionary<int, bool>();

        public double lastTime = 0;

        public WebGLShader GetShader(WebGLRenderingContext gl, string id)
        {
            var shaderScript = Document.GetElementById(id).As<HTMLScriptElement>();

            if (shaderScript == null)
            {
                return null;
            }

            var str = "";
            var k = shaderScript.FirstChild;

            while (k != null)
            {
                if (k.NodeType == NodeType.Text)
                {
                    str += k.TextContent;
                }

                k = k.NextSibling;
            }

            WebGLShader shader;

            if (shaderScript.Type == "x-shader/x-fragment")
            {
                shader = gl.CreateShader(gl.FRAGMENT_SHADER);
            }
            else if (shaderScript.Type == "x-shader/x-vertex")
            {
                shader = gl.CreateShader(gl.VERTEX_SHADER);
            }
            else
            {
                return null;
            }

            gl.ShaderSource(shader, str);
            gl.CompileShader(shader);

            if (!gl.GetShaderParameter(shader, gl.COMPILE_STATUS).As<bool>())
            {
                Global.Alert(gl.GetShaderInfoLog(shader));
                return null;
            }

            return shader;
        }

        public void Initialize(string canvasId)
        {
            InitSettings(this);

            this.canvas = GetCanvasEl(canvasId);
            WebGlRenderingContext = Create3DContext(this.canvas);

            InitShaders();
            InitBuffers();


            //cube.Tick();
            InitSettings(this);
        }

        public static HTMLCanvasElement GetCanvasEl(string id)
        {
            return Document.GetElementById(id).As<HTMLCanvasElement>();
        }

        public void InitShaders()
        {
            var fragmentShader = this.GetShader(WebGlRenderingContext, "shader-fs");
            var vertexShader = this.GetShader(WebGlRenderingContext, "shader-vs");
            var shaderProgram = WebGlRenderingContext.CreateProgram().As<WebGLProgram>();

            if (shaderProgram.Is<int>())
            {
                Global.Alert("Could not initialise program");
            }

            WebGlRenderingContext.AttachShader(shaderProgram, vertexShader);
            WebGlRenderingContext.AttachShader(shaderProgram, fragmentShader);
            WebGlRenderingContext.LinkProgram(shaderProgram);

            if (!WebGlRenderingContext.GetProgramParameter(shaderProgram, WebGlRenderingContext.LINK_STATUS).As<bool>())
            {
                Global.Alert("Could not initialise shaders");
            }

            WebGlRenderingContext.UseProgram(shaderProgram);

            this.vertexPositionAttribute = WebGlRenderingContext.GetAttribLocation(shaderProgram, "aVertexPosition");
            this.vertexNormalAttribute = WebGlRenderingContext.GetAttribLocation(shaderProgram, "aVertexNormal");
            this.textureCoordAttribute = WebGlRenderingContext.GetAttribLocation(shaderProgram, "aTextureCoord");

            WebGlRenderingContext.EnableVertexAttribArray(this.vertexPositionAttribute);
            WebGlRenderingContext.EnableVertexAttribArray(this.vertexNormalAttribute);
            WebGlRenderingContext.EnableVertexAttribArray(this.textureCoordAttribute);

            this.pMatrixUniform = WebGlRenderingContext.GetUniformLocation(shaderProgram, "uPMatrix");
            this.mvMatrixUniform = WebGlRenderingContext.GetUniformLocation(shaderProgram, "uMVMatrix");
            this.nMatrixUniform = WebGlRenderingContext.GetUniformLocation(shaderProgram, "uNMatrix");
            this.samplerUniform = WebGlRenderingContext.GetUniformLocation(shaderProgram, "uSampler");
            this.useLightingUniform = WebGlRenderingContext.GetUniformLocation(shaderProgram, "uUseLighting");
            this.ambientColorUniform = WebGlRenderingContext.GetUniformLocation(shaderProgram, "uAmbientColor");
            this.lightingDirectionUniform = WebGlRenderingContext.GetUniformLocation(shaderProgram, "uLightingDirection");
            this.directionalColorUniform = WebGlRenderingContext.GetUniformLocation(shaderProgram, "uDirectionalColor");
            this.alphaUniform = WebGlRenderingContext.GetUniformLocation(shaderProgram, "uAlpha");

            this.program = shaderProgram;
        }





        public void SetMatrixUniforms()
        {
            WebGlRenderingContext.UniformMatrix4fv(this.pMatrixUniform, false, pMatrix);
            WebGlRenderingContext.UniformMatrix4fv(this.mvMatrixUniform, false, mvMatrix);

            var normalMatrix = Mat3.Create();

            Mat4.ToInverseMat3(mvMatrix, normalMatrix);
            Mat3.Transpose(normalMatrix);

            WebGlRenderingContext.UniformMatrix3fv(this.nMatrixUniform, false, normalMatrix);
        }

        public double DegToRad(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public void HandleKeyDown(Event e)
        {
            this.currentlyPressedKeys[e.As<KeyboardEvent>().KeyCode] = true;
        }

        public void HandleKeyUp(Event e)
        {
            this.currentlyPressedKeys[e.As<KeyboardEvent>().KeyCode] = false;
        }

        public void HandleKeys()
        {
            if (CheckPressedKey(KeyboardEvent.DOM_VK_A))
            {
                ySpeed -= 1;
            }

            if (CheckPressedKey(KeyboardEvent.DOM_VK_D))
            {
                ySpeed += 1;
            }

            if (CheckPressedKey(KeyboardEvent.DOM_VK_W))
            {
                xSpeed -= 1;
            }

            if (CheckPressedKey(KeyboardEvent.DOM_VK_S))
            {
                xSpeed += 1;
            }
        }

        private bool CheckPressedKey(int key)
        {
            bool b = false;

            currentlyPressedKeys.TryGetValue(key, out b);

            return b;
        }

        public void InitBuffers()
        {
            this.cubeVertexPositionBuffer = WebGlRenderingContext.CreateBuffer();
            WebGlRenderingContext.BindBuffer(WebGlRenderingContext.ARRAY_BUFFER, cubeVertexPositionBuffer);

            var vertices = new double[] {
                // Front face
                -1.0, -1.0,  1.0,
                 1.0, -1.0,  1.0,
                 1.0,  1.0,  1.0,
                -1.0,  1.0,  1.0,

                // Back face
                -1.0, -1.0, -1.0,
                -1.0,  1.0, -1.0,
                 1.0,  1.0, -1.0,
                 1.0, -1.0, -1.0,

                // Top face
                -1.0,  1.0, -1.0,
                -1.0,  1.0,  1.0,
                 1.0,  1.0,  1.0,
                 1.0,  1.0, -1.0,

                // Bottom face
                -1.0, -1.0, -1.0,
                 1.0, -1.0, -1.0,
                 1.0, -1.0,  1.0,
                -1.0, -1.0,  1.0,

                // Right face
                 1.0, -1.0, -1.0,
                 1.0,  1.0, -1.0,
                 1.0,  1.0,  1.0,
                 1.0, -1.0,  1.0,

                // Left face
                -1.0, -1.0, -1.0,
                -1.0, -1.0,  1.0,
                -1.0,  1.0,  1.0,
                -1.0,  1.0, -1.0,
            };

            WebGlRenderingContext.BufferData(WebGlRenderingContext.ARRAY_BUFFER, new Float32Array(vertices), WebGlRenderingContext.STATIC_DRAW);

            this.cubeVertexNormalBuffer = WebGlRenderingContext.CreateBuffer();
            WebGlRenderingContext.BindBuffer(WebGlRenderingContext.ARRAY_BUFFER, cubeVertexNormalBuffer);

            var vertexNormals = new double[] {
                // Front face
                 0.0,  0.0,  1.0,
                 0.0,  0.0,  1.0,
                 0.0,  0.0,  1.0,
                 0.0,  0.0,  1.0,

                // Back face
                 0.0,  0.0, -1.0,
                 0.0,  0.0, -1.0,
                 0.0,  0.0, -1.0,
                 0.0,  0.0, -1.0,

                // Top face
                 0.0,  1.0,  0.0,
                 0.0,  1.0,  0.0,
                 0.0,  1.0,  0.0,
                 0.0,  1.0,  0.0,

                // Bottom face
                 0.0, -1.0,  0.0,
                 0.0, -1.0,  0.0,
                 0.0, -1.0,  0.0,
                 0.0, -1.0,  0.0,

                // Right face
                 1.0,  0.0,  0.0,
                 1.0,  0.0,  0.0,
                 1.0,  0.0,  0.0,
                 1.0,  0.0,  0.0,

                // Left face
                -1.0,  0.0,  0.0,
                -1.0,  0.0,  0.0,
                -1.0,  0.0,  0.0,
                -1.0,  0.0,  0.0
            };

            WebGlRenderingContext.BufferData(WebGlRenderingContext.ARRAY_BUFFER, new Float32Array(vertexNormals), WebGlRenderingContext.STATIC_DRAW);

            this.cubeVertexTextureCoordBuffer = WebGlRenderingContext.CreateBuffer();
            WebGlRenderingContext.BindBuffer(WebGlRenderingContext.ARRAY_BUFFER, cubeVertexTextureCoordBuffer);

            var textureCoords = new double[] {
                // Front face
                0.0, 0.0,
                1.0, 0.0,
                1.0, 1.0,
                0.0, 1.0,

                // Back face
                1.0, 0.0,
                1.0, 1.0,
                0.0, 1.0,
                0.0, 0.0,

                // Top face
                0.0, 1.0,
                0.0, 0.0,
                1.0, 0.0,
                1.0, 1.0,

                // Bottom face
                1.0, 1.0,
                0.0, 1.0,
                0.0, 0.0,
                1.0, 0.0,

                // Right face
                1.0, 0.0,
                1.0, 1.0,
                0.0, 1.0,
                0.0, 0.0,

                // Left face
                0.0, 0.0,
                1.0, 0.0,
                1.0, 1.0,
                0.0, 1.0
            };

            WebGlRenderingContext.BufferData(WebGlRenderingContext.ARRAY_BUFFER, new Float32Array(textureCoords), WebGlRenderingContext.STATIC_DRAW);

            this.cubeVertexIndexBuffer = WebGlRenderingContext.CreateBuffer();
            WebGlRenderingContext.BindBuffer(WebGlRenderingContext.ELEMENT_ARRAY_BUFFER, cubeVertexIndexBuffer);

            var cubeVertexIndices = new int[] {
                 0,  1,  2,    0,  2,  3,  // Front face
                 4,  5,  6,    4,  6,  7,  // Back face
                 8,  9, 10,    8, 10, 11,  // Top face
                12, 13, 14,   12, 14, 15,  // Bottom face
                16, 17, 18,   16, 18, 19,  // Right face
                20, 21, 22,   20, 22, 23   // Left face
            };

            WebGlRenderingContext.BufferData(WebGlRenderingContext.ELEMENT_ARRAY_BUFFER, new Uint16Array(cubeVertexIndices), WebGlRenderingContext.STATIC_DRAW);
        }

        void DrawInternal()
        {
            WebGlRenderingContext.Viewport(0, 0, canvas.Width, canvas.Height);
            WebGlRenderingContext.Clear(WebGlRenderingContext.COLOR_BUFFER_BIT | 
                WebGlRenderingContext.DEPTH_BUFFER_BIT);
            foreach(var sprite in Sprites)
            {
                DrawSprite(sprite);

            }
        }

        private void Scale(double[] matrix, float x, float y)
        {
            matrix[0] *= x;
            matrix[4] *= x;
            matrix[8] *= x;
            matrix[12] *= x;

            matrix[1] *= y;
            matrix[5] *= y;
            matrix[9] *= y;
            matrix[13] *= y;
        }

        private void DrawSprite(Sprite sprite)
        {

            Mat4.Perspective(45, (double)canvas.Width / canvas.Height, 0.1, 1000, pMatrix);
            Mat4.Identity(mvMatrix);
            Scale(mvMatrix, sprite.Width, sprite.Height);
            Mat4.Translate(mvMatrix, new double[] { sprite.X / sprite.Width, sprite.Y / sprite.Height, -12 });
            //Mat4.Rotate(mvMatrix, this.DegToRad(xRotation), new double[] { 1, 0, 0 });
            //Mat4.Rotate(mvMatrix, this.DegToRad(yRotation), new double[] { 0, 1, 0 });

            WebGlRenderingContext.BindBuffer(WebGlRenderingContext.ARRAY_BUFFER, this.cubeVertexPositionBuffer);
            WebGlRenderingContext.VertexAttribPointer(this.vertexPositionAttribute, 3, WebGlRenderingContext.FLOAT, false, 0, 0);

            WebGlRenderingContext.BindBuffer(WebGlRenderingContext.ARRAY_BUFFER, this.cubeVertexNormalBuffer);
            WebGlRenderingContext.VertexAttribPointer(this.vertexNormalAttribute, 3, WebGlRenderingContext.FLOAT, false, 0, 0);

            WebGlRenderingContext.BindBuffer(WebGlRenderingContext.ARRAY_BUFFER, this.cubeVertexTextureCoordBuffer);
            WebGlRenderingContext.VertexAttribPointer(this.textureCoordAttribute, 2, WebGlRenderingContext.FLOAT, false, 0, 0);

            WebGlRenderingContext.ActiveTexture(WebGlRenderingContext.TEXTURE0);
            WebGlRenderingContext.BindTexture(WebGlRenderingContext.TEXTURE_2D, sprite.Texture.WebGLTexture);

            WebGlRenderingContext.Uniform1i(this.samplerUniform, 0);

            // Add Blending
            if (this.useBlending)
            {
                WebGlRenderingContext.BlendFunc(WebGlRenderingContext.SRC_ALPHA, WebGlRenderingContext.ONE);
                WebGlRenderingContext.Enable(WebGlRenderingContext.BLEND);
                WebGlRenderingContext.Disable(WebGlRenderingContext.DEPTH_TEST);
                WebGlRenderingContext.Uniform1f(this.alphaUniform, this.alpha);
            }
            else
            {
                WebGlRenderingContext.Disable(WebGlRenderingContext.BLEND);
                WebGlRenderingContext.Enable(WebGlRenderingContext.DEPTH_TEST);
                WebGlRenderingContext.Uniform1f(this.alphaUniform, 1);
            }

            // Add Lighting
            WebGlRenderingContext.Uniform1i(this.useLightingUniform, this.useLighting);

            if (this.useLighting)
            {
                WebGlRenderingContext.Uniform3f(this.ambientColorUniform, this.ambientR, this.ambientG, this.ambientB);

                var lightingDirection = new double[] { this.lightDirectionX, this.lightDirectionY, this.lightDirectionZ };
                var adjustedLD = Vec3.Create();

                Vec3.Normalize(lightingDirection, adjustedLD);
                Vec3.Scale(adjustedLD, -1);

                WebGlRenderingContext.Uniform3fv(this.lightingDirectionUniform, adjustedLD);
                WebGlRenderingContext.Uniform3f(this.directionalColorUniform, this.directionalR, this.directionalG, this.directionalB);
            }

            WebGlRenderingContext.BindBuffer(WebGlRenderingContext.ELEMENT_ARRAY_BUFFER, this.cubeVertexIndexBuffer);

            this.SetMatrixUniforms();

            WebGlRenderingContext.DrawElements(WebGlRenderingContext.TRIANGLES, 6, WebGlRenderingContext.UNSIGNED_SHORT, 0);
        }

        public void Animate()
        {
            var timeNow = new Date().GetTime();

            if (this.lastTime != 0)
            {
                var elapsed = timeNow - this.lastTime;

                this.xRotation += (this.xSpeed * elapsed) / 1000.0;
                this.yRotation += (this.ySpeed * elapsed) / 1000.0;
            }

            this.lastTime = timeNow;
        }

        public void Draw()
        {
            this.HandleKeys();
            this.DrawInternal();
            this.Animate();

        }

        public void Tick()
        {
            Global.SetTimeout(this.Tick, 20);
        }

        void InitSettings(Renderer cube)
        {
            var useSettings = Document.GetElementById("settings").As<HTMLInputElement>();

            if (useSettings == null || !useSettings.Checked)
            {
                return;
            }

            cube.useBlending = Document.GetElementById("blending").As<HTMLInputElement>().Checked;
            cube.alpha = Global.ParseFloat(Document.GetElementById("alpha").As<HTMLInputElement>().Value);

            cube.useLighting = Document.GetElementById("lighting").As<HTMLInputElement>().Checked;

            cube.ambientR = Global.ParseFloat(Document.GetElementById("ambientR").As<HTMLInputElement>().Value);
            cube.ambientG = Global.ParseFloat(Document.GetElementById("ambientG").As<HTMLInputElement>().Value);
            cube.ambientB = Global.ParseFloat(Document.GetElementById("ambientB").As<HTMLInputElement>().Value);

            cube.lightDirectionX = Global.ParseFloat(Document.GetElementById("lightDirectionX").As<HTMLInputElement>().Value);
            cube.lightDirectionY = Global.ParseFloat(Document.GetElementById("lightDirectionY").As<HTMLInputElement>().Value);
            cube.lightDirectionZ = Global.ParseFloat(Document.GetElementById("lightDirectionZ").As<HTMLInputElement>().Value);

            cube.directionalR = Global.ParseFloat(Document.GetElementById("directionalR").As<HTMLInputElement>().Value);
            cube.directionalG = Global.ParseFloat(Document.GetElementById("directionalG").As<HTMLInputElement>().Value);
            cube.directionalB = Global.ParseFloat(Document.GetElementById("directionalB").As<HTMLInputElement>().Value);

        }

        public static WebGLRenderingContext Create3DContext(HTMLCanvasElement canvas)
        {
            string[] names = new string[]
            {
                "webgl",
                "experimental-webgl",
                "webkit-3d",
                "moz-webgl"
            };

            WebGLRenderingContext context = null;

            foreach (string name in names)
            {
                try
                {
                    context = canvas.GetContext(name).As<WebGLRenderingContext>();
                }
                catch { }

                if (context != null)
                {
                    break;
                }
            }

            return context;
        }
    }
}
