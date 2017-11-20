using Bridge;
using Bridge.GLMatrix;
using Bridge.Html5;
using Bridge.WebGL;
using System;
using System.Collections.Generic;

namespace Cube3D
{
    public class Cube
    {
        public HTMLCanvasElement canvas;
        public WebGLRenderingContext gl;
        public WebGLProgram program;
        public WebGLTexture texture;

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
        public string textureImageSrc = "crate.gif";

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

        public double z = -5.0;
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

        public void Initialize()
        {
            InitSettings(this);

            this.canvas = GetCanvasEl(Game1.CanvasId);
            this.gl = Create3DContext(this.canvas);

            InitShaders();
            InitBuffers();
            InitTexture();

            //cube.Tick();
            InitSettings(this);
        }

        public static HTMLCanvasElement GetCanvasEl(string id)
        {
            return Document.GetElementById(id).As<HTMLCanvasElement>();
        }

        public void InitShaders()
        {
            var fragmentShader = this.GetShader(gl, "shader-fs");
            var vertexShader = this.GetShader(gl, "shader-vs");
            var shaderProgram = gl.CreateProgram().As<WebGLProgram>();

            if (shaderProgram.Is<int>())
            {
                Global.Alert("Could not initialise program");
            }

            gl.AttachShader(shaderProgram, vertexShader);
            gl.AttachShader(shaderProgram, fragmentShader);
            gl.LinkProgram(shaderProgram);

            if (!gl.GetProgramParameter(shaderProgram, gl.LINK_STATUS).As<bool>())
            {
                Global.Alert("Could not initialise shaders");
            }

            gl.UseProgram(shaderProgram);

            this.vertexPositionAttribute = gl.GetAttribLocation(shaderProgram, "aVertexPosition");
            this.vertexNormalAttribute = gl.GetAttribLocation(shaderProgram, "aVertexNormal");
            this.textureCoordAttribute = gl.GetAttribLocation(shaderProgram, "aTextureCoord");

            gl.EnableVertexAttribArray(this.vertexPositionAttribute);
            gl.EnableVertexAttribArray(this.vertexNormalAttribute);
            gl.EnableVertexAttribArray(this.textureCoordAttribute);

            this.pMatrixUniform = gl.GetUniformLocation(shaderProgram, "uPMatrix");
            this.mvMatrixUniform = gl.GetUniformLocation(shaderProgram, "uMVMatrix");
            this.nMatrixUniform = gl.GetUniformLocation(shaderProgram, "uNMatrix");
            this.samplerUniform = gl.GetUniformLocation(shaderProgram, "uSampler");
            this.useLightingUniform = gl.GetUniformLocation(shaderProgram, "uUseLighting");
            this.ambientColorUniform = gl.GetUniformLocation(shaderProgram, "uAmbientColor");
            this.lightingDirectionUniform = gl.GetUniformLocation(shaderProgram, "uLightingDirection");
            this.directionalColorUniform = gl.GetUniformLocation(shaderProgram, "uDirectionalColor");
            this.alphaUniform = gl.GetUniformLocation(shaderProgram, "uAlpha");

            this.program = shaderProgram;
        }

        public void HandleLoadedTexture(HTMLImageElement image)
        {
            gl.PixelStorei(gl.UNPACK_FLIP_Y_WEBGL, true);
            gl.BindTexture(gl.TEXTURE_2D, this.texture);
            gl.TexImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image);
            gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
            gl.TexParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_NEAREST);
            gl.GenerateMipmap(gl.TEXTURE_2D);
            gl.BindTexture(gl.TEXTURE_2D, null);
        }

        public void InitTexture()
        {
            this.texture = gl.CreateTexture();

            var textureImageElement = new HTMLImageElement();

            textureImageElement.OnLoad = (ev) =>
            {
                this.HandleLoadedTexture(textureImageElement);
            };

            textureImageElement.Src = this.textureImageSrc;
        }

        public void SetMatrixUniforms()
        {
            gl.UniformMatrix4fv(this.pMatrixUniform, false, pMatrix);
            gl.UniformMatrix4fv(this.mvMatrixUniform, false, mvMatrix);

            var normalMatrix = Mat3.Create();

            Mat4.ToInverseMat3(mvMatrix, normalMatrix);
            Mat3.Transpose(normalMatrix);

            gl.UniformMatrix3fv(this.nMatrixUniform, false, normalMatrix);
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
            if (CheckPressedKey(KeyboardEvent.DOM_VK_Q))
            {
                z -= 0.05;
            }

            if (CheckPressedKey(KeyboardEvent.DOM_VK_E))
            {
                z += 0.05;
            }

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
            this.cubeVertexPositionBuffer = gl.CreateBuffer();
            gl.BindBuffer(gl.ARRAY_BUFFER, cubeVertexPositionBuffer);

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

            gl.BufferData(gl.ARRAY_BUFFER, new Float32Array(vertices), gl.STATIC_DRAW);

            this.cubeVertexNormalBuffer = gl.CreateBuffer();
            gl.BindBuffer(gl.ARRAY_BUFFER, cubeVertexNormalBuffer);

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

            gl.BufferData(gl.ARRAY_BUFFER, new Float32Array(vertexNormals), gl.STATIC_DRAW);

            this.cubeVertexTextureCoordBuffer = gl.CreateBuffer();
            gl.BindBuffer(gl.ARRAY_BUFFER, cubeVertexTextureCoordBuffer);

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

            gl.BufferData(gl.ARRAY_BUFFER, new Float32Array(textureCoords), gl.STATIC_DRAW);

            this.cubeVertexIndexBuffer = gl.CreateBuffer();
            gl.BindBuffer(gl.ELEMENT_ARRAY_BUFFER, cubeVertexIndexBuffer);

            var cubeVertexIndices = new int[] {
                 0,  1,  2,    0,  2,  3,  // Front face
                 4,  5,  6,    4,  6,  7,  // Back face
                 8,  9, 10,    8, 10, 11,  // Top face
                12, 13, 14,   12, 14, 15,  // Bottom face
                16, 17, 18,   16, 18, 19,  // Right face
                20, 21, 22,   20, 22, 23   // Left face
            };

            gl.BufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(cubeVertexIndices), gl.STATIC_DRAW);
        }

        public void DrawScene()
        {
            gl.Viewport(0, 0, canvas.Width, canvas.Height);
            gl.Clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);

            Mat4.Perspective(45, (double)canvas.Width / canvas.Height, 0.1, 100, pMatrix);
            Mat4.Identity(mvMatrix);
            Mat4.Translate(mvMatrix, new double[] { 0.0, 0.0, z });
            Mat4.Rotate(mvMatrix, this.DegToRad(xRotation), new double[] { 1, 0, 0 });
            Mat4.Rotate(mvMatrix, this.DegToRad(yRotation), new double[] { 0, 1, 0 });

            gl.BindBuffer(gl.ARRAY_BUFFER, this.cubeVertexPositionBuffer);
            gl.VertexAttribPointer(this.vertexPositionAttribute, 3, gl.FLOAT, false, 0, 0);

            gl.BindBuffer(gl.ARRAY_BUFFER, this.cubeVertexNormalBuffer);
            gl.VertexAttribPointer(this.vertexNormalAttribute, 3, gl.FLOAT, false, 0, 0);

            gl.BindBuffer(gl.ARRAY_BUFFER, this.cubeVertexTextureCoordBuffer);
            gl.VertexAttribPointer(this.textureCoordAttribute, 2, gl.FLOAT, false, 0, 0);

            gl.ActiveTexture(gl.TEXTURE0);
            gl.BindTexture(gl.TEXTURE_2D, this.texture);

            gl.Uniform1i(this.samplerUniform, 0);

            // Add Blending
            if (this.useBlending)
            {
                gl.BlendFunc(gl.SRC_ALPHA, gl.ONE);
                gl.Enable(gl.BLEND);
                gl.Disable(gl.DEPTH_TEST);
                gl.Uniform1f(this.alphaUniform, this.alpha);
            }
            else
            {
                gl.Disable(gl.BLEND);
                gl.Enable(gl.DEPTH_TEST);
                gl.Uniform1f(this.alphaUniform, 1);
            }

            // Add Lighting
            gl.Uniform1i(this.useLightingUniform, this.useLighting);

            if (this.useLighting)
            {
                gl.Uniform3f(this.ambientColorUniform, this.ambientR, this.ambientG, this.ambientB);

                var lightingDirection = new double[] { this.lightDirectionX, this.lightDirectionY, this.lightDirectionZ };
                var adjustedLD = Vec3.Create();

                Vec3.Normalize(lightingDirection, adjustedLD);
                Vec3.Scale(adjustedLD, -1);

                gl.Uniform3fv(this.lightingDirectionUniform, adjustedLD);
                gl.Uniform3f(this.directionalColorUniform, this.directionalR, this.directionalG, this.directionalB);
            }

            gl.BindBuffer(gl.ELEMENT_ARRAY_BUFFER, this.cubeVertexIndexBuffer);

            this.SetMatrixUniforms();

            gl.DrawElements(gl.TRIANGLES, 36, gl.UNSIGNED_SHORT, 0);
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

        public void Update()
        {
            this.HandleKeys();
            this.DrawScene();
            this.Animate();

        }

        public void Tick()
        {
            Global.SetTimeout(this.Tick, 20);
        }

        public static void InitSettings(Cube cube)
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

            cube.textureImageSrc = "crate.gif";
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
