/**
 * @version 1.0.0.0
 * @copyright Copyright ©  2017
 * @compiler Bridge.NET 16.0.0-beta5
 */
Bridge.assembly("FlatRedBridge", function ($asm, globals) {
    "use strict";

    Bridge.define("FlatRedBall.Sprite", {
        fields: {
            texture: null
        },
        props: {
            Texture: {
                get: function () {
                    return this.texture;
                },
                set: function (value) {
                    this.texture = value;
                    this.UpdateToTextureScale();
                }
            },
            Width: 0,
            Height: 0,
            X: 0,
            Y: 0
        },
        ctors: {
            init: function () {
                this.Width = 5.0;
                this.Height = 10;
            }
        },
        methods: {
            UpdateToTextureScale: function () {
                //Console.WriteLine("UpdateToTextureScale");
                //this.Width = texture.Width;
                //this.Height = texture.Height;
            }
        }
    });

    Bridge.define("Microsoft.Xna.Framework.Graphics.Texture2D", {
        props: {
            Name: null,
            WebGLTexture: null,
            Width: 0,
            Height: 0
        },
        methods: {
            InitTexture: function (gl, file) {
                this.Name = file;
                this.WebGLTexture = gl.createTexture();

                var textureImageElement = new Image();

                textureImageElement.onload = Bridge.fn.bind(this, function (ev) {
                    this.HandleLoadedTexture(textureImageElement, gl);

                    this.Width = textureImageElement.width;
                    this.Height = textureImageElement.height;

                    System.Console.WriteLine("OnLoad");

                });

                textureImageElement.src = file;
            },
            HandleLoadedTexture: function (image, gl) {
                gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, true);
                gl.bindTexture(gl.TEXTURE_2D, this.WebGLTexture);
                gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image);

                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_NEAREST);
                gl.generateMipmap(gl.TEXTURE_2D);
                gl.bindTexture(gl.TEXTURE_2D, null);


            }
        }
    });
});

//# sourceMappingURL=data:application/json;base64,ewogICJ2ZXJzaW9uIjogMywKICAiZmlsZSI6ICJGbGF0UmVkQnJpZGdlLmpzIiwKICAic291cmNlUm9vdCI6ICIiLAogICJzb3VyY2VzIjogWyJTcHJpdGUuY3MiLCJUZXh0dXJlMkQuY3MiXSwKICAibmFtZXMiOiBbIiJdLAogICJtYXBwaW5ncyI6ICI7Ozs7Ozs7Ozs7Ozs7OztvQkFja0JBLE9BQU9BOzs7b0JBQ1BBLGVBQVVBO29CQUFPQTs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7Ozs7OzttQ0NHSEEsSUFBMEJBO2dCQUU5Q0EsWUFBT0E7Z0JBQ1BBLG9CQUFvQkE7O2dCQUVwQkEsMEJBQTBCQTs7Z0JBRTFCQSw2QkFBNkJBLCtCQUFDQTtvQkFFMUJBLHlCQUF5QkEscUJBQXFCQTs7b0JBRTlDQSxhQUFRQTtvQkFDUkEsY0FBU0E7O29CQUVUQTs7OztnQkFJSkEsMEJBQTBCQTs7MkNBR0xBLE9BQXdCQTtnQkFFN0NBLGVBQWVBO2dCQUNmQSxlQUFlQSxlQUFlQTtnQkFDOUJBLGNBQWNBLGtCQUFrQkEsU0FBU0EsU0FBU0Esa0JBQWtCQTs7Z0JBRXBFQSxpQkFBaUJBLGVBQWVBLHVCQUF1QkE7Z0JBQ3ZEQSxpQkFBaUJBLGVBQWVBLHVCQUF1QkE7Z0JBQ3ZEQSxrQkFBa0JBO2dCQUNsQkEsZUFBZUEsZUFBZUEiLAogICJzb3VyY2VzQ29udGVudCI6IFsidXNpbmcgTWljcm9zb2Z0LlhuYS5GcmFtZXdvcmsuR3JhcGhpY3M7XHJcbnVzaW5nIFN5c3RlbTtcclxudXNpbmcgU3lzdGVtLkNvbGxlY3Rpb25zLkdlbmVyaWM7XHJcbnVzaW5nIFN5c3RlbS5MaW5xO1xyXG51c2luZyBTeXN0ZW0uVGV4dDtcclxudXNpbmcgU3lzdGVtLlRocmVhZGluZy5UYXNrcztcclxuXHJcbm5hbWVzcGFjZSBGbGF0UmVkQmFsbFxyXG57XHJcbiAgICBwdWJsaWMgY2xhc3MgU3ByaXRlXHJcbiAgICB7XHJcbiAgICAgICAgVGV4dHVyZTJEIHRleHR1cmU7XHJcbiAgICAgICAgcHVibGljIFRleHR1cmUyRCBUZXh0dXJlXHJcbiAgICAgICAge1xyXG4gICAgICAgICAgICBnZXQgeyByZXR1cm4gdGV4dHVyZTsgfVxyXG4gICAgICAgICAgICBzZXQgeyB0ZXh0dXJlID0gdmFsdWU7IFVwZGF0ZVRvVGV4dHVyZVNjYWxlKCk7IH1cclxuICAgICAgICB9XHJcblxyXG4gICAgICAgIHB1YmxpYyBmbG9hdCBXaWR0aCB7IGdldDsgc2V0OyB9XHJcbiAgICAgICAgcHVibGljIGZsb2F0IEhlaWdodCB7IGdldDsgc2V0OyB9XHJcblxyXG4gICAgICAgIHB1YmxpYyBmbG9hdCBYIHsgZ2V0OyBzZXQ7IH1cclxuICAgICAgICBwdWJsaWMgZmxvYXQgWSB7IGdldDsgc2V0OyB9XHJcblxyXG4gICAgICAgIHByaXZhdGUgdm9pZCBVcGRhdGVUb1RleHR1cmVTY2FsZSgpXHJcbiAgICAgICAge1xyXG4gICAgICAgICAgICAvL0NvbnNvbGUuV3JpdGVMaW5lKFwiVXBkYXRlVG9UZXh0dXJlU2NhbGVcIik7XHJcbiAgICAgICAgICAgIC8vdGhpcy5XaWR0aCA9IHRleHR1cmUuV2lkdGg7XHJcbiAgICAgICAgICAgIC8vdGhpcy5IZWlnaHQgPSB0ZXh0dXJlLkhlaWdodDtcclxuICAgICAgICB9XHJcblxuICAgIFxucHJpdmF0ZSBmbG9hdCBfX1Byb3BlcnR5X19Jbml0aWFsaXplcl9fV2lkdGg9NWY7cHJpdmF0ZSBmbG9hdCBfX1Byb3BlcnR5X19Jbml0aWFsaXplcl9fSGVpZ2h0PTEwO31cclxufVxyXG4iLCJ1c2luZyBCcmlkZ2UuSHRtbDU7XHJcbnVzaW5nIEJyaWRnZS5XZWJHTDtcclxudXNpbmcgU3lzdGVtO1xyXG51c2luZyBTeXN0ZW0uQ29sbGVjdGlvbnMuR2VuZXJpYztcclxudXNpbmcgU3lzdGVtLkxpbnE7XHJcbnVzaW5nIFN5c3RlbS5UZXh0O1xyXG51c2luZyBTeXN0ZW0uVGhyZWFkaW5nLlRhc2tzO1xyXG5cclxubmFtZXNwYWNlIE1pY3Jvc29mdC5YbmEuRnJhbWV3b3JrLkdyYXBoaWNzXHJcbntcclxuICAgIHB1YmxpYyBjbGFzcyBUZXh0dXJlMkRcclxuICAgIHtcclxuICAgICAgICBwdWJsaWMgc3RyaW5nIE5hbWUgeyBnZXQ7IHByaXZhdGUgc2V0OyB9XHJcbiAgICAgICAgcHVibGljIFdlYkdMVGV4dHVyZSBXZWJHTFRleHR1cmUgeyBnZXQ7IHByaXZhdGUgc2V0OyB9XG5cbiAgICAgICAgcHVibGljIGludCBXaWR0aCB7IGdldDsgcHJpdmF0ZSBzZXQ7IH1cbiAgICAgICAgcHVibGljIGludCBIZWlnaHQgeyBnZXQ7IHByaXZhdGUgc2V0OyB9XG5cclxuICAgICAgICBwdWJsaWMgdm9pZCBJbml0VGV4dHVyZShXZWJHTFJlbmRlcmluZ0NvbnRleHQgZ2wsIHN0cmluZyBmaWxlKVxuICAgICAgICB7XG4gICAgICAgICAgICBOYW1lID0gZmlsZTtcbiAgICAgICAgICAgIHRoaXMuV2ViR0xUZXh0dXJlID0gZ2wuQ3JlYXRlVGV4dHVyZSgpO1xuXG4gICAgICAgICAgICB2YXIgdGV4dHVyZUltYWdlRWxlbWVudCA9IG5ldyBIVE1MSW1hZ2VFbGVtZW50KCk7XG5cbiAgICAgICAgICAgIHRleHR1cmVJbWFnZUVsZW1lbnQuT25Mb2FkID0gKGV2KSA9PlxuICAgICAgICAgICAge1xuICAgICAgICAgICAgICAgIHRoaXMuSGFuZGxlTG9hZGVkVGV4dHVyZSh0ZXh0dXJlSW1hZ2VFbGVtZW50LCBnbCk7XG5cbiAgICAgICAgICAgICAgICBXaWR0aCA9IHRleHR1cmVJbWFnZUVsZW1lbnQuV2lkdGg7XG4gICAgICAgICAgICAgICAgSGVpZ2h0ID0gdGV4dHVyZUltYWdlRWxlbWVudC5IZWlnaHQ7XHJcblxyXG4gICAgICAgICAgICAgICAgQ29uc29sZS5Xcml0ZUxpbmUoXCJPbkxvYWRcIik7XHJcblxyXG4gICAgICAgICAgICB9O1xuXG4gICAgICAgICAgICB0ZXh0dXJlSW1hZ2VFbGVtZW50LlNyYyA9IGZpbGU7XG4gICAgICAgIH1cclxuXHJcbiAgICAgICAgdm9pZCBIYW5kbGVMb2FkZWRUZXh0dXJlKEhUTUxJbWFnZUVsZW1lbnQgaW1hZ2UsIFdlYkdMUmVuZGVyaW5nQ29udGV4dCBnbClcbiAgICAgICAge1xuICAgICAgICAgICAgZ2wuUGl4ZWxTdG9yZWkoZ2wuVU5QQUNLX0ZMSVBfWV9XRUJHTCwgdHJ1ZSk7XG4gICAgICAgICAgICBnbC5CaW5kVGV4dHVyZShnbC5URVhUVVJFXzJELCB0aGlzLldlYkdMVGV4dHVyZSk7XG4gICAgICAgICAgICBnbC5UZXhJbWFnZTJEKGdsLlRFWFRVUkVfMkQsIDAsIGdsLlJHQkEsIGdsLlJHQkEsIGdsLlVOU0lHTkVEX0JZVEUsIGltYWdlKTtcblxuICAgICAgICAgICAgZ2wuVGV4UGFyYW1ldGVyaShnbC5URVhUVVJFXzJELCBnbC5URVhUVVJFX01BR19GSUxURVIsIGdsLkxJTkVBUik7XG4gICAgICAgICAgICBnbC5UZXhQYXJhbWV0ZXJpKGdsLlRFWFRVUkVfMkQsIGdsLlRFWFRVUkVfTUlOX0ZJTFRFUiwgZ2wuTElORUFSX01JUE1BUF9ORUFSRVNUKTtcbiAgICAgICAgICAgIGdsLkdlbmVyYXRlTWlwbWFwKGdsLlRFWFRVUkVfMkQpO1xuICAgICAgICAgICAgZ2wuQmluZFRleHR1cmUoZ2wuVEVYVFVSRV8yRCwgbnVsbCk7XG5cblxuICAgICAgICB9XHJcbiAgICB9XHJcbn1cclxuIl0KfQo=
