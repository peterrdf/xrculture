/*
 * Custom event handlers
 */
var g_onSelectObjectEvent = null
var g_onHoverObjectEvent = null
var g_getObjectDescription = null

Array.prototype.clean = function (deleteValue) {
    for (let i = 0; i < this.length; i++) {
        if (this[i] === deleteValue) {
            this.splice(i, 1)
            i--
        }
    }
    return this
}

/*
 * Upload file status
 */
const UPLOAD_EVENT_BEGIN = 0
const UPLOAD_EVENT_PROGRESS = 1
const UPLOAD_EVENT_CONVERT = 2
const UPLOAD_EVENT_END = 3

/*
 * View
 */
const DEFAULT_VIEW = 0
const FRONT_VIEW = 1
const BACK_VIEW = 2
const TOP_VIEW = 3
const BOTTOP_VIEW = 4
const LEFT_VIEW = 5
const RIGHT_VIEW = 6

const NAVIGATION_VIEW_LENGTH = 200;
const MIN_VIEW_PORT_LENGTH = 100;

/*
 * Viewer
 */
var Viewer = function () {
    /**************************************************************************
     * Members
     */

    /*
     * On-line mode
     */
    this._isOnline = false

    /*
     * Shader
     */
    this._shaderProgram = null

    /*
     * Matrices
     */
    this._mtxModelView = mat4.create()
    this._mtxProjection = mat4.create()
    this._mtxInversePMV = mat4.create()
    this._updateProjectionMatrix = true
    this._applyTranslations = true

    /*
     * Scene
     */
    this._clearColor = [0.9, 0.9, 0.9, 1.0]
    this._pointLightPosition = vec3.create([0.25, 0.25, 1])
    this._materialShininess = 50.0
    this._defaultEyeVector = [0, 0, -5]
    this._eyeVector = vec3.create(this._defaultEyeVector)
    this._rotateX = 30
    this._rotateY = 30

    /*
     * Selection
     */
    this._instancesSelectionColors = []
    this._pickedObject = -1
    this._selectedObjects = []

    /*
     * World
     */
    this._worldDimensions = { Xmin: -0.5, Ymin: -0.5, Zmin: -0.5, Xmax: 0.5, Ymax: 0.5, Zmax: 0.5, MaxDistance: 1.0 }

    /*
     * Textures
     */
    this._defaultTexture = null
    this._textures = {}

    /*
     * Scale
     */
    this._scaleFactor = 1.0

    /*
     * Selection support
     */
    this._selectionFramebuffer = null
    this._selectionTexture
    this._selectedPixelValues = new Uint8Array(4)

    /*
     * Fly to object
     */
    this._flyToObjectData = null;

    /*
     * Measures
     */
    this._divHeight = null;
    this._txtHeight = null;
    this._divWidth = null;
    this._txtWidth = null;
    this._divDepth = null;
    this._txtDepth = null;

    /*
     * Tooltip
     */
    this._divTooltip = null;
    this._txtTooltip = null;

    /*
     * Texture
     */
    this.canvas = null

    /*
     * Visibility
     */
    this._viewTriangles = VIEW_TRIANGLES;
    this._viewWireframes = VIEW_WIREFRAMES;
    this._viewLines = VIEW_LINES;
    this._viewPoints = VIEW_POINTS;
    this._viewCoordinateSystem = VIEW_COORDINATE_SYSTEM;
    this._viewNavigator = VIEW_NAVIGATOR;
    this._viewGrid = VIEW_GRID;
    this._viewBBox = VIEW_BBOX;
    this._viewBBoxX = VIEW_BBOX_X;
    this._viewBBoxY = VIEW_BBOX_Y;
    this._viewBBoxZ = VIEW_BBOX_Z;
    this._viewTootlip = VIEW_TOOLTIP;

    /*
     * Grid
     */
    this._gridLinesCount = 10;
    this._gridVBO = null;

    /**************************************************************************
     * General
     */

    /*
     * Initialize
     */
    Viewer.prototype.initProgram = function () {
        var fgShader = utils.getShader(gl, 'shader-fs')
        var vxShader = utils.getShader(gl, 'shader-vs')

        this._shaderProgram = gl.createProgram()
        gl.attachShader(this._shaderProgram, vxShader)
        gl.attachShader(this._shaderProgram, fgShader)
        gl.linkProgram(this._shaderProgram)

        if (!gl.getProgramParameter(this._shaderProgram, gl.LINK_STATUS)) {
            alert('Could not initialize shaders.')
            console.error('Could not initialize shaders.')

            return false
        }

        gl.useProgram(this._shaderProgram)

        /* Vertex Shader */
        this._shaderProgram.VertexPosition = gl.getAttribLocation(
            this._shaderProgram,
            'Position'
        )

        this._shaderProgram.VertexNormal = gl.getAttribLocation(
            this._shaderProgram,
            'Normal'
        )

        this._shaderProgram.UV = gl.getAttribLocation(
            this._shaderProgram,
            'UV'
        )

        this._shaderProgram.ProjectionMatrix = gl.getUniformLocation(
            this._shaderProgram,
            'ProjectionMatrix'
        )
        this._shaderProgram.ModelViewMatrix = gl.getUniformLocation(
            this._shaderProgram,
            'ModelViewMatrix'
        )

        this._shaderProgram.NormalMatrix = gl.getUniformLocation(
            this._shaderProgram,
            'NormalMatrix'
        )

        this._shaderProgram.DiffuseMaterial = gl.getUniformLocation(
            this._shaderProgram,
            'DiffuseMaterial'
        )

        this._shaderProgram.EnableLighting = gl.getUniformLocation(
            this._shaderProgram,
            'EnableLighting'
        )

        this._shaderProgram.EnableTexture = gl.getUniformLocation(
            this._shaderProgram,
            'EnableTexture'
        )

        /* Fragment Shader */
        this._shaderProgram.LightPosition = gl.getUniformLocation(
            this._shaderProgram,
            'LightPosition'
        )

        this._shaderProgram.AmbientMaterial = gl.getUniformLocation(
            this._shaderProgram,
            'AmbientMaterial'
        )

        this._shaderProgram.SpecularMaterial = gl.getUniformLocation(
            this._shaderProgram,
            'SpecularMaterial'
        )

        this._shaderProgram.Transparency = gl.getUniformLocation(
            this._shaderProgram,
            'Transparency'
        )

        // #todo
        //this._shaderProgram.uMaterialEmissiveColor = gl.getUniformLocation(
        //  this._shaderProgram,
        //  'uMaterialEmissiveColor'
        //)

        this._shaderProgram.Shininess = gl.getUniformLocation(
            this._shaderProgram,
            'Shininess'
        )

        this._shaderProgram.AmbientLightWeighting = gl.getUniformLocation(
            this._shaderProgram,
            'AmbientLightWeighting'
        )

        this._shaderProgram.DiffuseLightWeighting = gl.getUniformLocation(
            this._shaderProgram,
            'DiffuseLightWeighting'
        )

        this._shaderProgram.SpecularLightWeighting = gl.getUniformLocation(
            this._shaderProgram,
            'SpecularLightWeighting'
        )

        this._shaderProgram.Sampler = gl.getUniformLocation(
            this._shaderProgram,
            'Sampler'
        )

        this._defaultTexture = this.createTexture('texture.png')

        return true
    }

    /*
     * Lights
     */
    Viewer.prototype.setLights = function () {
        gl.uniform3f(
            this._shaderProgram.LightPosition,
            this._pointLightPosition[0],
            this._pointLightPosition[1],
            this._pointLightPosition[2]
        )

        gl.uniform1f(
            this._shaderProgram.Shininess,
            this._materialShininess
        )

        gl.uniform3f(
            this._shaderProgram.AmbientLightWeighting,
            0.4, 0.4, 0.4)

        gl.uniform3f(
            this._shaderProgram.DiffuseLightWeighting,
            0.95, 0.95, 0.95)

        gl.uniform3f(
            this._shaderProgram.SpecularLightWeighting,
            0.15, 0.15, 0.15)
    }

    /*
     * Initialize
     */
    Viewer.prototype.init = function (canvasID, width, height) {
        gl = utils.getGLContext(canvasID)
        if (!gl) {
            alert('Could not initialize WebGL.')
            console.error('Could not initialize WebGL.')

            return false
        }

        if (!this.initProgram()) {
            return false
        }

        // Fix for WARNING: there is no texture bound to the unit 0
        function createTexture(type, target, count) {
            var data = new Uint8Array(4) // 4 is required to match default unpack alignment of 4.
            var texture = gl.createTexture()

            gl.bindTexture(type, texture)
            gl.texParameteri(type, gl.TEXTURE_MIN_FILTER, gl.NEAREST)
            gl.texParameteri(type, gl.TEXTURE_MAG_FILTER, gl.NEAREST)

            for (let i = 0; i < count; i++) {
                gl.texImage2D(
                    target + i,
                    0,
                    gl.RGBA,
                    1,
                    1,
                    0,
                    gl.RGBA,
                    gl.UNSIGNED_BYTE,
                    data
                )
            }

            return texture
        }

        var emptyTextures = {}
        emptyTextures[gl.TEXTURE_2D] = createTexture(
            gl.TEXTURE_2D,
            gl.TEXTURE_2D,
            1)
        emptyTextures[gl.TEXTURE_CUBE_MAP] = createTexture(
            gl.TEXTURE_CUBE_MAP,
            gl.TEXTURE_CUBE_MAP_POSITIVE_X,
            6)

        gl.activeTexture(gl.TEXTURE0)
        gl.bindTexture(gl.TEXTURE_2D, emptyTextures[gl.TEXTURE_2D])

        gl.activeTexture(gl.TEXTURE1)
        gl.bindTexture(gl.TEXTURE_CUBE_MAP, emptyTextures[gl.TEXTURE_CUBE_MAP])
        // END WARNING: there is no texture bound to the unit 0

        resizeCanvas(width, height)

        this.initSelectionFramebuffer()

        this.loadInstances()

        this.setLights()

        renderLoop()

        return true
    }

    /**
     * Default Projection matrix
     */
    Viewer.prototype.setDefultProjectionMatrix = function () {
        if (!this._updateProjectionMatrix) {
            return
        }

        /*
         * Projection matrix
         */
        mat4.identity(this._mtxProjection)
        mat4.perspective(
            45,
            gl.canvas.width / gl.canvas.height,
            0.001,
            1000000.0,
            this._mtxProjection
        )

        gl.uniformMatrix4fv(
            this._shaderProgram.ProjectionMatrix,
            false,
            this._mtxProjection
        )
    }

    /**
     * Default Model-View, Inverse Model-View and Normal matrices
     */
    Viewer.prototype.setDefultMatrices = function () {
        /*
         * Projection matrix
         */
        this.setDefultProjectionMatrix()

        /*
         * Model-View matrix
         */
        mat4.identity(this._mtxModelView)

        if (this._applyTranslations) {
            mat4.translate(this._mtxModelView, this._eyeVector)
        }
        else {
            mat4.translate(this._mtxModelView, [0, 0, -5])
        }

        mat4.multiply(this._mtxProjection, this._mtxModelView, this._mtxInversePMV)
        mat4.inverse(this._mtxInversePMV)

        mat4.rotate(this._mtxModelView, (this._rotateX * Math.PI) / 180, [1, 0, 0])
        mat4.rotate(this._mtxModelView, (this._rotateY * Math.PI) / 180, [0, 1, 0])

        /*
         * Fit the image
         */
        if (this._applyTranslations) {
            // [0.0 -> X/Y/Zmin + X/Y/Zmax]
            mat4.translate(this._mtxModelView, [
                -this._worldDimensions.Xmin,
                -this._worldDimensions.Ymin,
                -this._worldDimensions.Zmin,
            ])

            // Center
            mat4.translate(this._mtxModelView, [
                -(this._worldDimensions.Xmax - this._worldDimensions.Xmin) / 2,
                -(this._worldDimensions.Ymax - this._worldDimensions.Ymin) / 2,
                -(this._worldDimensions.Zmax - this._worldDimensions.Zmin) / 2,
            ])
        }

        gl.uniformMatrix4fv(
            this._shaderProgram.ModelViewMatrix,
            false,
            this._mtxModelView
        )

        /*
         * Normal matrix
         */
        gl.uniformMatrix3fv(this._shaderProgram.NormalMatrix, false, mat4.toMat3(this._mtxModelView))
    }

    /**
     * Draws the scene
     */
    Viewer.prototype.drawScene = function () {
        this.setDefultMatrices()

        gl.viewport(0, 0, gl.canvas.width, gl.canvas.height)
        gl.clearColor(
            this._clearColor[0],
            this._clearColor[1],
            this._clearColor[2],
            this._clearColor[3]
        )
        gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT)

        gl.enable(gl.SAMPLE_COVERAGE)
        gl.sampleCoverage(1.0, false)

        gl.enable(gl.DEPTH_TEST)
        gl.depthFunc(gl.LEQUAL)

        this.setLights()
        this.drawInstances()
        this.drawSceneInstances()
        this.drawNavigatorInstances()
        this.drawInstancesSelectionFrameBuffer()
    }

    /**
     * Selection support
     */
    Viewer.prototype.initSelectionFramebuffer = function () {
        this._selectionFramebuffer = gl.createFramebuffer()
        gl.bindFramebuffer(gl.FRAMEBUFFER, this._selectionFramebuffer)
        this._selectionFramebuffer.width = 512
        this._selectionFramebuffer.height = 512

        this._selectionTexture = gl.createTexture()
        gl.bindTexture(gl.TEXTURE_2D, this._selectionTexture)
        gl.texImage2D(
            gl.TEXTURE_2D,
            0,
            gl.RGBA,
            this._selectionFramebuffer.width,
            this._selectionFramebuffer.height,
            0,
            gl.RGBA,
            gl.UNSIGNED_BYTE,
            null)

        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR)
        gl.texParameteri(
            gl.TEXTURE_2D,
            gl.TEXTURE_MIN_FILTER,
            gl.LINEAR_MIPMAP_NEAREST
        )
        gl.generateMipmap(gl.TEXTURE_2D)

        var renderbuffer = gl.createRenderbuffer()
        gl.bindRenderbuffer(gl.RENDERBUFFER, renderbuffer)
        gl.renderbufferStorage(
            gl.RENDERBUFFER,
            gl.DEPTH_COMPONENT16,
            this._selectionFramebuffer.width,
            this._selectionFramebuffer.height
        )

        gl.framebufferTexture2D(
            gl.FRAMEBUFFER,
            gl.COLOR_ATTACHMENT0,
            gl.TEXTURE_2D,
            this._selectionTexture,
            0
        )
        gl.framebufferRenderbuffer(
            gl.FRAMEBUFFER,
            gl.DEPTH_ATTACHMENT,
            gl.RENDERBUFFER,
            renderbuffer
        )

        gl.bindTexture(gl.TEXTURE_2D, null)
        gl.bindRenderbuffer(gl.RENDERBUFFER, null)
        gl.bindFramebuffer(gl.FRAMEBUFFER, null)
    }

    /*
     * Interaction support
     */
    Viewer.prototype.pickObject = function (x, y) {
        if (this._selectionFramebuffer === null) {
            return
        }

        let pickedObject = this._pickedObject

        this._pickedObject = -1

        if (this._divTooltip !== null) {
            this._divTooltip.style.display = 'none'
        }

        gl.bindFramebuffer(gl.FRAMEBUFFER, this._selectionFramebuffer)
        gl.readPixels(
            x * (this._selectionFramebuffer.width / gl.canvas.width),
            (gl.canvas.height - y) *
            (this._selectionFramebuffer.height / gl.canvas.height),
            1,
            1,
            gl.RGBA,
            gl.UNSIGNED_BYTE,
            this._selectedPixelValues
        )
        gl.bindFramebuffer(gl.FRAMEBUFFER, null)

        if (this._selectedPixelValues[3] !== 0.0) {
            // decoding of the selection color
            var objectIndex =
                this._selectedPixelValues[0 /*R*/] * (255 * 255) +
                this._selectedPixelValues[1 /*G*/] * 255 +
                this._selectedPixelValues[2 /*B*/]

            this._pickedObject = objectIndex
        } // if (this._selectedPixelValues[3] != ...

        if (g_onHoverObjectEvent !== null) {
            g_onHoverObjectEvent(
                this._pickedObject !== -1 ? g_instances[this._pickedObject - 1] : null
            )
        }

        if (this._viewTootlip && (g_getObjectDescription !== null) && (this._pickedObject !== -1)) {
            let description = g_getObjectDescription(g_instances[this._pickedObject - 1])
            if (description !== null) {
                const divContainerElement = document.querySelector("#labels-container");

                if (this._divTooltip === null) {
                    this._divTooltip = document.createElement("div")
                    this._divTooltip.className = "floating-div tooltip-div"

                    this._txtTooltip = document.createTextNode("")
                    this._divTooltip.appendChild(this._txtTooltip)

                    divContainerElement.appendChild(this._divTooltip)
                }

                this._divTooltip.style.left = (x + 10) + 'px'
                this._divTooltip.style.top = (y - 10) + 'px'

                this._txtTooltip.nodeValue = description

                this._divTooltip.style.display = 'block'
            }
        }

        if (this._pickedObject !== pickedObject) {
            PENDING_DRAW_SCENE = true
        }
    }

    /*
     * Interaction support
     */
    Viewer.prototype.selectObject = function (x, y) {
        if (this._selectionFramebuffer === null) {
            return
        }

        gl.bindFramebuffer(gl.FRAMEBUFFER, this._selectionFramebuffer)
        gl.readPixels(
            x * (this._selectionFramebuffer.width / gl.canvas.width),
            (gl.canvas.height - y) *
            (this._selectionFramebuffer.height / gl.canvas.height),
            1,
            1,
            gl.RGBA,
            gl.UNSIGNED_BYTE,
            this._selectedPixelValues
        )
        gl.bindFramebuffer(gl.FRAMEBUFFER, null)

        if (this._selectedPixelValues[3] !== 0.0) {
            // decoding of the selection color
            var objectIndex =
                this._selectedPixelValues[0 /*R*/] * (255 * 255) +
                this._selectedPixelValues[1 /*G*/] * 255 +
                this._selectedPixelValues[2 /*B*/]

            if (MULTI_SELECTION_MODE) {
                var index = this._selectedObjects.indexOf(objectIndex)
                if (index === -1) {
                    // Add the object if it doesn't exist
                    this._selectedObjects.push(objectIndex)
                } else {
                    // Remove it
                    this._selectedObjects.splice(index, 1)
                }
            } // if (MULTI_SELECTION_MODE)
            else {
                this._selectedObjects = []
                this._selectedObjects.push(objectIndex)
            }
        } // if (this._selectedPixelValues[3] != ...
        else {
            // Reset
            this._selectedObjects = []
        }

        // Custom event handler
        if (g_onSelectObjectEvent !== null) {
            g_onSelectObjectEvent(
                this._selectedObjects.length > 0
                    ? g_instances[this._selectedObjects[0] - 1]
                    : null
            )
        }

        PENDING_DRAW_SCENE = true
    }

    /*
     * Select
     */
    Viewer.prototype.selectObjectByIndex = function (index) {
        this._selectedObjects = []

        if ((index >= 0) && (index < g_instances.length)) {
            this._selectedObjects.push(index + 1)
            this.zoomToObject(g_instances[index])
        }

        PENDING_DRAW_SCENE = true
    }

    /*
     * Select
     */
    Viewer.prototype.selectObjectByIndexes = function (indexes) {
        this._selectedObjects = []

        const indexesToSelect = indexes
            .filter((index) => index >= 0 && index < g_instances.length)
            .map((index) => index + 1)
        this._selectedObjects.push(...indexesToSelect)

        PENDING_DRAW_SCENE = true
    }

    /*
     * Select
     */
    Viewer.prototype.selectObjectByGuid = function (guid) {
        this._selectedObjects = []

        for (let i = 0; i < g_instances.length; i++) {
            if (g_instances[i].guid === guid) {
                this._selectedObjects.push(i + 1)
                this.zoomToObject(g_instances[i])
                return
            }
        }

        PENDING_DRAW_SCENE = true
    }

    /*
     * Select; integrator.js
     */
    Viewer.prototype.selectObjectByUri = function (uri) {
        this._selectedObjects = []

        for (let i = 0; i < g_instances.length; i++) {
            if (g_instances[i].uri === uri) {
                this._selectedObjects.push(i + 1)
                this.zoomToObject(g_instances[i])
                return
            }
        }

        PENDING_DRAW_SCENE = true
    }

    /**
     * Loads a texture
     */
    Viewer.prototype.createTexture = function (textureFile) {
        try {
            var viewer = this

            var texture = gl.createTexture()

            // Temp texture until the image is loaded
            // https://stackoverflow.com/questions/19722247/webgl-wait-for-texture-to-load/19748905#19748905
            gl.bindTexture(gl.TEXTURE_2D, texture)
            gl.texImage2D(
                gl.TEXTURE_2D,
                0,
                gl.RGBA,
                1,
                1,
                0,
                gl.RGBA,
                gl.UNSIGNED_BYTE,
                new Uint8Array([0, 0, 0, 255]))

            var image = new Image()
            image.addEventListener('error', function () {
                console.error("Can't load '" + textureFile + "'.")
            })

            image.addEventListener('load', function () {
                gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, true)
                // Now that the image has loaded make copy it to the texture.
                gl.bindTexture(gl.TEXTURE_2D, texture)
                gl.texImage2D(
                    gl.TEXTURE_2D,
                    0,
                    gl.RGBA,
                    gl.RGBA,
                    gl.UNSIGNED_BYTE,
                    image)

                // Check if the image is a power of 2 in both dimensions.
                if (viewer.isPowerOf2(image.width) && viewer.isPowerOf2(image.height)) {
                    // Yes, it's a power of 2. Generate mips.			
                    gl.generateMipmap(gl.TEXTURE_2D);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR)
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR)
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.REPEAT)
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.REPEAT)
                } else {
                    // No, it's not a power of 2. Turn of mips and set wrapping to clamp to edge
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE)
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE)
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR)
                }

                gl.bindTexture(gl.TEXTURE_2D, null)

                PENDING_DRAW_SCENE = true
            })

            image.src = textureFile

            return texture
        } catch (ex) {
            console.error(ex)
        }

        return null
    }

    /**
     * Loads a texture
     */
    Viewer.prototype.createTextureBase64 = function (base64Content) {
        try {
            var viewer = this

            var texture = gl.createTexture()

            // Temp texture until the image is loaded
            // https://stackoverflow.com/questions/19722247/webgl-wait-for-texture-to-load/19748905#19748905
            gl.bindTexture(gl.TEXTURE_2D, texture)
            gl.texImage2D(
                gl.TEXTURE_2D,
                0,
                gl.RGBA,
                1,
                1,
                0,
                gl.RGBA,
                gl.UNSIGNED_BYTE,
                new Uint8Array([0, 0, 0, 255])
            )

            var image = new Image()
            image.addEventListener('error', function () {
                console.error("Can't load the texture.")
            })

            image.addEventListener('load', function () {
                gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, true)
                // Now that the image has loaded make copy it to the texture.
                gl.bindTexture(gl.TEXTURE_2D, texture)

                // Check if the image is a power of 2 in both dimensions.
                if (viewer.isPowerOf2(image.width) && viewer.isPowerOf2(image.height)) {
                    gl.texImage2D(
                        gl.TEXTURE_2D,
                        0,
                        gl.RGBA,
                        gl.RGBA,
                        gl.UNSIGNED_BYTE,
                        image)

                    // Yes, it's a power of 2. Generate mips.
                    gl.generateMipmap(gl.TEXTURE_2D)
                    gl.texParameteri(
                        gl.TEXTURE_2D,
                        gl.TEXTURE_MIN_FILTER,
                        gl.LINEAR_MIPMAP_LINEAR)
                } else {
                    // No, it's not a power of 2. Resize
                    image = viewer.makePowerOfTwo(image)

                    gl.texImage2D(
                        gl.TEXTURE_2D,
                        0,
                        gl.RGBA,
                        gl.RGBA,
                        gl.UNSIGNED_BYTE,
                        image)

                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR)
                }

                gl.bindTexture(gl.TEXTURE_2D, null)

                PENDING_DRAW_SCENE = true
            })

            image.src = base64Content

            return texture
        } catch (ex) {
            console.error(ex)
        }

        return null
    }

    /**
     * Loads a texture
     */
    Viewer.prototype.createTextureBLOB = function (blob, flipY) {
        try {
            var viewer = this

            var texture = gl.createTexture()

            // Temp texture until the image is loaded
            // https://stackoverflow.com/questions/19722247/webgl-wait-for-texture-to-load/19748905#19748905
            gl.bindTexture(gl.TEXTURE_2D, texture)
            gl.texImage2D(
                gl.TEXTURE_2D,
                0,
                gl.RGBA,
                1,
                1,
                0,
                gl.RGBA,
                gl.UNSIGNED_BYTE,
                new Uint8Array([0, 0, 0, 255]))

            var image = new Image()
            image.addEventListener('error', function () {
                console.error("Can't load the texture.")
            })

            image.addEventListener('load', function () {
                gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, flipY)
                // Now that the image has loaded make copy it to the texture.
                gl.bindTexture(gl.TEXTURE_2D, texture)

                // Check if the image is a power of 2 in both dimensions.
                if (viewer.isPowerOf2(image.width) && viewer.isPowerOf2(image.height)) {
                    gl.texImage2D(
                        gl.TEXTURE_2D,
                        0,
                        gl.RGBA,
                        gl.RGBA,
                        gl.UNSIGNED_BYTE,
                        image)

                    // Yes, it's a power of 2. Generate mips.
                    gl.generateMipmap(gl.TEXTURE_2D);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR)
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR)
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.REPEAT)
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.REPEAT)
                } else {
                    // No, it's not a power of 2. Resize
                    image = viewer.makePowerOfTwo(image)

                    gl.texImage2D(
                        gl.TEXTURE_2D,
                        0,
                        gl.RGBA,
                        gl.RGBA,
                        gl.UNSIGNED_BYTE,
                        image)
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR)
                }

                gl.bindTexture(gl.TEXTURE_2D, null)

                PENDING_DRAW_SCENE = true
            })

            image.src = URL.createObjectURL(blob)

            return texture
        } catch (ex) {
            console.error(ex)
        }

        return null
    }

    /**
     * Texture support
     */
    Viewer.prototype.isPowerOf2 = function (value) {
        return (value & (value - 1)) === 0
    }

    Viewer.prototype.floorPowerOfTwo = function (value) {
        return Math.pow(2, Math.floor(Math.log(value) / Math.LN2))
    }

    Viewer.prototype.makePowerOfTwo = function (image) {
        if (
            image instanceof HTMLImageElement ||
            image instanceof HTMLCanvasElement
        ) {
            if (this.canvas === null)
                this.canvas = document.createElementNS(
                    'http://www.w3.org/1999/xhtml',
                    'canvas'
                )

            this.canvas.width = this.floorPowerOfTwo(image.width)
            this.canvas.height = this.floorPowerOfTwo(image.height)

            var context = this.canvas.getContext('2d')
            context.drawImage(image, 0, 0, this.canvas.width, this.canvas.height)

            return this.canvas
        }

        return image
    }

    Viewer.prototype.zoomTo = function (x, y) {
        if (this._selectionFramebuffer === null) {
            return
        }

        gl.bindFramebuffer(gl.FRAMEBUFFER, this._selectionFramebuffer)
        gl.readPixels(
            x * (this._selectionFramebuffer.width / gl.canvas.width),
            (gl.canvas.height - y) *
            (this._selectionFramebuffer.height / gl.canvas.height),
            1,
            1,
            gl.RGBA,
            gl.UNSIGNED_BYTE,
            this._selectedPixelValues
        )
        gl.bindFramebuffer(gl.FRAMEBUFFER, null)

        if (this._selectedPixelValues[3] !== 0.0) {
            // decoding of the selection color
            var objectIndex =
                this._selectedPixelValues[0 /*R*/] * (255 * 255) +
                this._selectedPixelValues[1 /*G*/] * 255 +
                this._selectedPixelValues[2 /*B*/]

            this.zoomToObject(g_instances[objectIndex - 1])
        } // if (this._selectedPixelValues[3] != ...
    }

    Viewer.prototype.zoomToObject = function (instance) {
        if (instance === null) {
            return
        }

        this.beginFlyToObject(instance)
    }

    Viewer.prototype.beginFlyToObject = function (instance) {
        PENDING_DRAW_SCENE = true
        PENDING_DRAW_SCENE_COUNT = FLY_TO_OBJECT_FRAMES_COUNT

        setTimeout(() => {
            this._worldDimensions.Xmin -= this._eyeVector[0]
            this._worldDimensions.Xmax += this._eyeVector[0]
            this._worldDimensions.Ymin -= this._eyeVector[1]
            this._worldDimensions.Ymax += this._eyeVector[1]
            this._worldDimensions.Zmin -= this._eyeVector[2]
            this._worldDimensions.Zmax += this._eyeVector[2]

            this._worldDimensions.EyeVectorX = this._eyeVector[0]
            this._worldDimensions.EyeVectorY = this._eyeVector[1]
            this._worldDimensions.EyeVectorZ = this._eyeVector[2]

            this._flyToObjectData = {}
            this._flyToObjectData.Xmin = instance.Xmin
            this._flyToObjectData.Xmax = instance.Xmax
            this._flyToObjectData.Ymin = instance.Ymin
            this._flyToObjectData.Ymax = instance.Ymax
            this._flyToObjectData.Zmin = instance.Zmin
            this._flyToObjectData.Zmax = instance.Zmax

            this._flyToObjectData.MaxDistance = this._flyToObjectData.Xmax - this._flyToObjectData.Xmin
            this._flyToObjectData.MaxDistance = Math.max(this._flyToObjectData.MaxDistance, this._flyToObjectData.Ymax - this._flyToObjectData.Ymin)
            this._flyToObjectData.MaxDistance = Math.max(this._flyToObjectData.MaxDistance, this._flyToObjectData.Zmax - this._worldDimensions.Zmin)

            this._flyToObjectData.EyeVectorX = 0
            this._flyToObjectData.EyeVectorY = 0
            this._flyToObjectData.EyeVectorZ = -(2 * this._flyToObjectData.MaxDistance)

            this._flyToObjectData.XMinStep = Math.abs(this._worldDimensions.Xmin - instance.Xmin) / FLY_TO_OBJECT_STEPS
            this._flyToObjectData.XMaxStep = Math.abs(this._worldDimensions.Xmax - instance.Xmax) / FLY_TO_OBJECT_STEPS
            this._flyToObjectData.YMinStep = Math.abs(this._worldDimensions.Ymin - instance.Ymin) / FLY_TO_OBJECT_STEPS
            this._flyToObjectData.YMaxStep = Math.abs(this._worldDimensions.Ymax - instance.Ymax) / FLY_TO_OBJECT_STEPS
            this._flyToObjectData.ZMinStep = Math.abs(this._worldDimensions.Zmin - instance.Zmin) / FLY_TO_OBJECT_STEPS
            this._flyToObjectData.ZMaxStep = Math.abs(this._worldDimensions.Zmax - instance.Zmax) / FLY_TO_OBJECT_STEPS

            this._flyToObjectData.EyeVectorXStep = Math.abs(this._worldDimensions.EyeVectorX - this._flyToObjectData.EyeVectorX) / FLY_TO_OBJECT_STEPS
            this._flyToObjectData.EyeVectorYStep = Math.abs(this._worldDimensions.EyeVectorY - this._flyToObjectData.EyeVectorY) / FLY_TO_OBJECT_STEPS
            this._flyToObjectData.EyeVectorZStep = Math.abs(this._worldDimensions.EyeVectorZ - this._flyToObjectData.EyeVectorZ) / FLY_TO_OBJECT_STEPS

            if (this.updateFlyToObjectData()) {
                this.flyToObject()
            }
        }, FLY_TO_OBJECT_TIMEOUT)
    }

    Viewer.prototype.flyToObject = function () {
        if (this._flyToObjectData === null) {
            return
        }

        setTimeout(() => {
            if (this.updateFlyToObjectData()) {
                this.flyToObject()
            }
        }, FLY_TO_OBJECT_TIMEOUT)
    }

    Viewer.prototype.updateFlyToObjectData = function () {
        if (this._flyToObjectData === null) {
            return false
        }

        let pendingUpdate = this.updateFlyToObjectDataValue('Xmin', 'XMinStep')
        pendingUpdate |= this.updateFlyToObjectDataValue('Xmax', 'XMaxStep')
        pendingUpdate |= this.updateFlyToObjectDataValue('Ymin', 'YMinStep')
        pendingUpdate |= this.updateFlyToObjectDataValue('Ymax', 'YMaxStep')
        pendingUpdate |= this.updateFlyToObjectDataValue('Zmin', 'ZMinStep')
        pendingUpdate |= this.updateFlyToObjectDataValue('Zmax', 'ZMaxStep')
        pendingUpdate |= this.updateFlyToObjectDataValue('EyeVectorX', 'EyeVectorXStep')
        pendingUpdate |= this.updateFlyToObjectDataValue('EyeVectorY', 'EyeVectorYStep')
        pendingUpdate |= this.updateFlyToObjectDataValue('EyeVectorZ', 'EyeVectorZStep')

        if (pendingUpdate) {
            this._defaultEyeVector = vec3.create([
                this._worldDimensions.EyeVectorX,
                this._worldDimensions.EyeVectorY,
                this._worldDimensions.EyeVectorZ])

            this._eyeVector = vec3.create(this._defaultEyeVector)

            return true
        }

        this._flyToObjectData = null
        return false
    }

    Viewer.prototype.updateFlyToObjectDataValue = function (value, step) {
        if (this._worldDimensions[value] === this._flyToObjectData[value]) {
            return false
        }

        if (this._worldDimensions[value] < this._flyToObjectData[value]) {
            this._worldDimensions[value] = this._worldDimensions[value] + this._flyToObjectData[step]

            if (this._worldDimensions[value] > this._flyToObjectData[value]) {
                this._worldDimensions[value] = this._flyToObjectData[value]
                return false
            }
        }
        else {
            if (this._worldDimensions[value] > this._flyToObjectData[value]) {
                this._worldDimensions[value] = this._worldDimensions[value] - this._flyToObjectData[step]

                if (this._worldDimensions[value] < this._flyToObjectData[value]) {
                    this._worldDimensions[value] = this._flyToObjectData[value]
                    return false
                }
            }
        }

        return true
    }

    Viewer.prototype.zoomToObjectNoFly = function (instance) {
        if (instance === null) {
            return
        }

        this._worldDimensions.Xmin = instance.Xmin
        this._worldDimensions.Xmax = instance.Xmax
        this._worldDimensions.Ymin = instance.Ymin
        this._worldDimensions.Ymax = instance.Ymax
        this._worldDimensions.Zmin = instance.Zmin
        this._worldDimensions.Zmax = instance.Zmax

        this._worldDimensions.MaxDistance =
            this._worldDimensions.Xmax - this._worldDimensions.Xmin
        this._worldDimensions.MaxDistance = Math.max(
            this._worldDimensions.MaxDistance,
            this._worldDimensions.Ymax - this._worldDimensions.Ymin
        )
        this._worldDimensions.MaxDistance = Math.max(
            this._worldDimensions.MaxDistance,
            this._worldDimensions.Zmax - this._worldDimensions.Zmin
        )

        this._defaultEyeVector[2] = -(2 * this._worldDimensions.MaxDistance)
        this._eyeVector = vec3.create(this._defaultEyeVector)
    }

    Viewer.prototype.reset = function () {
        try {
            this.resetView()

            this._selectedObjects = []
            this._pickedObject = -1
        } catch (ex) {
            console.error()
        }
    }

    /*
     * Default view
     */
    Viewer.prototype.resetView = function () {
        this._clearColor = [0.9, 0.9, 0.9, 1.0]

        this._pointLightPosition = vec3.create([0.25, 0.25, 1])
        this._materialShininess = 50.0

        this._defaultEyeVector = [0, 0, -5]
        this._eyeVector = vec3.create(this._defaultEyeVector)

        /*
         * Calculate world's dimensions
         */
        if (g_instances.length > 0) {
            this._worldDimensions.Xmin = g_instances[0].Xmin
            this._worldDimensions.Xmax = g_instances[0].Xmax
            this._worldDimensions.Ymin = g_instances[0].Ymin
            this._worldDimensions.Ymax = g_instances[0].Ymax
            this._worldDimensions.Zmin = g_instances[0].Zmin
            this._worldDimensions.Zmax = g_instances[0].Zmax

            for (let i = 1; i < g_instances.length; i++) {
                this._worldDimensions.Xmin = Math.min(
                    this._worldDimensions.Xmin,
                    g_instances[i].Xmin
                )
                this._worldDimensions.Ymin = Math.min(
                    this._worldDimensions.Ymin,
                    g_instances[i].Ymin
                )
                this._worldDimensions.Zmin = Math.min(
                    this._worldDimensions.Zmin,
                    g_instances[i].Zmin
                )

                this._worldDimensions.Xmax = Math.max(
                    this._worldDimensions.Xmax,
                    g_instances[i].Xmax
                )
                this._worldDimensions.Ymax = Math.max(
                    this._worldDimensions.Ymax,
                    g_instances[i].Ymax
                )
                this._worldDimensions.Zmax = Math.max(
                    this._worldDimensions.Zmax,
                    g_instances[i].Zmax
                )
            } // for (let i = ...

            this._worldDimensions.MaxDistance =
                this._worldDimensions.Xmax - this._worldDimensions.Xmin
            this._worldDimensions.MaxDistance = Math.max(
                this._worldDimensions.MaxDistance,
                this._worldDimensions.Ymax - this._worldDimensions.Ymin
            )
            this._worldDimensions.MaxDistance = Math.max(
                this._worldDimensions.MaxDistance,
                this._worldDimensions.Zmax - this._worldDimensions.Zmin
            )

            this._defaultEyeVector[2] = -(2 * this._worldDimensions.MaxDistance)
            this._eyeVector = vec3.create(this._defaultEyeVector)
        } // if (g_instances.length > 0)

        this._rotateX = 30
        this._rotateY = 30

        PENDING_DRAW_SCENE = true
    }

    /*
    * View
    */
    Viewer.prototype.setView = function (view) {
        this.resetView()

        switch (view) {
            case DEFAULT_VIEW: {
                // NA
            }
                break;

            case FRONT_VIEW: {
                this._rotateX = 0
                this._rotateY = 0
            }
                break;

            case BACK_VIEW: {
                this._rotateX = 0
                this._rotateY = 180
            }
                break;

            case TOP_VIEW: {
                this._rotateX = 270
                this._rotateY = 0
            }
                break;

            case BOTTOP_VIEW: {
                this._rotateX = 90
                this._rotateY = 0
            }
                break;

            case LEFT_VIEW: {
                this._rotateX = 0
                this._rotateY = 270
            }
                break;

            case RIGHT_VIEW: {
                this._rotateX = 0
                this._rotateY = 90
            }
                break;
        } // switch (view)

        PENDING_DRAW_SCENE = true
    }

    /**************************************************************************
     * Instances
     */

    /*
     * Cleanup
     */
    Viewer.prototype.deleteBuffers = function () {
        console.info('deleteBuffers - BEGIN')

        let count = 0

        for (let i = 0; i < g_instances.length; i++) {
            if (g_instances[i].BBVBO) {
                gl.deleteBuffer(g_instances[i].BBVBO)
                count++
            }
        }

        for (let i = 0; i < g_geometries.length; i++) {
            let geometry = g_geometries[i]

            if (geometry.VBO) {
                // CRASH!?!?
                //gl.deleteBuffer(geometry.VBO)
                count++
            }

            for (let j = 0; j < geometry.conceptualFaces.length; j++) {
                if (geometry.conceptualFaces[j].IBO) {
                    gl.deleteBuffer(geometry.conceptualFaces[j].IBO)
                    count++
                }

                if (geometry.conceptualFacesPolygons[j].IBO) {
                    gl.deleteBuffer(geometry.conceptualFacesPolygons[j].IBO)
                    count++
                }

                if (geometry.conceptualFaces[j].IBOLines) {
                    gl.deleteBuffer(geometry.conceptualFaces[j].IBOLines)
                    count++
                }

                if (geometry.conceptualFaces[j].IBOPoints) {
                    gl.deleteBuffer(geometry.conceptualFaces[j].IBOPoints)
                    count++
                }
            } // for (let j = ...
        } // for (let i = ...

        if (this._gridVBO) {
            gl.deleteBuffer(this._gridVBO)
            count++
        }

        console.info('deleteBuffers - END: ' + count)
    }

    /*
     * Load
     */
    Viewer.prototype.loadInstances = function () {
        console.info('loadInstances - BEGIN')

        this._instancesSelectionColors = []
        this._pickedObject = -1
        this._selectedObjects = []
        this._worldDimensions = {}
        this._gridVBO = null

        /*
        * Geometry
        */
        try {
            for (let i = 0; i < g_geometries.length; i++) {
                let geometry = g_geometries[i]

                if (!geometry.vertices) {
                    console.error('Unknown data model.')
                    continue
                }

                if (geometry.VBO) {
                    // Already loaded
                    continue
                }

                /*
                 * VBOs
                 */
                let vertexBufferObject = gl.createBuffer()
                gl.bindBuffer(gl.ARRAY_BUFFER, vertexBufferObject)
                gl.bufferData(
                    gl.ARRAY_BUFFER,
                    new Float32Array(geometry.vertices),
                    gl.STATIC_DRAW)

                vertexBufferObject.length = geometry.vertices.length
                geometry.VBO = vertexBufferObject

                /*
                 * IBO-s
                 */
                for (let j = 0; j < geometry.conceptualFaces.length; j++) {
                    /*
                    * IBO - Triangles
                    */
                    if (geometry.conceptualFaces[j].indicesTriangles &&
                        (geometry.conceptualFaces[j].indicesTriangles.length > 0)) {
                        let indexBufferObject = gl.createBuffer()
                        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, indexBufferObject)
                        gl.bufferData(
                            gl.ELEMENT_ARRAY_BUFFER,
                            new Uint16Array(geometry.conceptualFaces[j].indicesTriangles),
                            gl.STATIC_DRAW)

                        indexBufferObject.count = geometry.conceptualFaces[j].indicesTriangles.length
                        geometry.conceptualFaces[j].IBO = indexBufferObject
                    }

                    /*
                    * IBO - Conceptual faces polygons
                    */
                    if (geometry.conceptualFacesPolygons[j].indices &&
                        (geometry.conceptualFacesPolygons[j].indices.length > 0)) {
                        let indexBufferObject = gl.createBuffer()
                        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, indexBufferObject)
                        gl.bufferData(
                            gl.ELEMENT_ARRAY_BUFFER,
                            new Uint16Array(geometry.conceptualFacesPolygons[j].indices),
                            gl.STATIC_DRAW)

                        indexBufferObject.count = geometry.conceptualFacesPolygons[j].indices.length
                        geometry.conceptualFacesPolygons[j].IBO = indexBufferObject
                    }

                    /*
                    * IBO - Lines
                    */
                    if (geometry.conceptualFaces[j].indicesLines &&
                        (geometry.conceptualFaces[j].indicesLines.length > 0)) {
                        let indexBufferObject = gl.createBuffer()
                        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, indexBufferObject)
                        gl.bufferData(
                            gl.ELEMENT_ARRAY_BUFFER,
                            new Uint16Array(geometry.conceptualFaces[j].indicesLines),
                            gl.STATIC_DRAW)

                        indexBufferObject.count = geometry.conceptualFaces[j].indicesLines.length
                        geometry.conceptualFaces[j].IBOLines = indexBufferObject
                    }

                    /*
                    * IBO - Points
                    */
                    if (geometry.conceptualFaces[j].indicesPoints &&
                        (geometry.conceptualFaces[j].indicesPoints.length > 0)) {
                        let indexBufferObject = gl.createBuffer()
                        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, indexBufferObject)
                        gl.bufferData(
                            gl.ELEMENT_ARRAY_BUFFER,
                            new Uint16Array(geometry.conceptualFaces[j].indicesPoints),
                            gl.STATIC_DRAW)

                        indexBufferObject.count = geometry.conceptualFaces[j].indicesPoints.length
                        geometry.conceptualFaces[j].IBOPoints = indexBufferObject
                    }
                } // for (let j = ...
            } // for (let i = ...

            /*
             * Calculate world's dimensions
             */
            if (g_instances.length > 0) {
                this._worldDimensions.Xmin = g_instances[0].Xmin
                this._worldDimensions.Xmax = g_instances[0].Xmax
                this._worldDimensions.Ymin = g_instances[0].Ymin
                this._worldDimensions.Ymax = g_instances[0].Ymax
                this._worldDimensions.Zmin = g_instances[0].Zmin
                this._worldDimensions.Zmax = g_instances[0].Zmax

                for (let i = 1; i < g_instances.length; i++) {
                    this._worldDimensions.Xmin = Math.min(
                        this._worldDimensions.Xmin,
                        g_instances[i].Xmin
                    )
                    this._worldDimensions.Ymin = Math.min(
                        this._worldDimensions.Ymin,
                        g_instances[i].Ymin
                    )
                    this._worldDimensions.Zmin = Math.min(
                        this._worldDimensions.Zmin,
                        g_instances[i].Zmin
                    )

                    this._worldDimensions.Xmax = Math.max(
                        this._worldDimensions.Xmax,
                        g_instances[i].Xmax
                    )
                    this._worldDimensions.Ymax = Math.max(
                        this._worldDimensions.Ymax,
                        g_instances[i].Ymax
                    )
                    this._worldDimensions.Zmax = Math.max(
                        this._worldDimensions.Zmax,
                        g_instances[i].Zmax
                    )
                } // for (let i = ...

                this._worldDimensions.MaxDistance =
                    this._worldDimensions.Xmax - this._worldDimensions.Xmin
                this._worldDimensions.MaxDistance = Math.max(
                    this._worldDimensions.MaxDistance,
                    this._worldDimensions.Ymax - this._worldDimensions.Ymin
                )
                this._worldDimensions.MaxDistance = Math.max(
                    this._worldDimensions.MaxDistance,
                    this._worldDimensions.Zmax - this._worldDimensions.Zmin
                )

                if (this._worldDimensions.MaxDistance === 0) {
                    this._worldDimensions = { Xmin: -0.5, Ymin: -0.5, Zmin: -0.5, Xmax: 0.5, Ymax: 0.5, Zmax: 0.5, MaxDistance: 2.0 }
                }
            } // if (g_instances.length > 0)
            else {
                this._worldDimensions = { Xmin: -0.5, Ymin: -0.5, Zmin: -0.5, Xmax: 0.5, Ymax: 0.5, Zmax: 0.5, MaxDistance: 2.0 }
            }

            this._defaultEyeVector[2] = -(2 * this._worldDimensions.MaxDistance)
            this._eyeVector = vec3.create(this._defaultEyeVector)

            /*
             * Default selection
             */
            for (let i = 0; i < DEFAULT_SELECTED_OBJECTS.length; i++) {
                var index = g_instances.findIndex(function (v) {
                    return v.uri === DEFAULT_SELECTED_OBJECTS[i]
                })

                if (index !== -1) {
                    this._selectedObjects.push(index + 1)
                }
            } // for (let i = ...
        } catch (e) {
            console.error(e)
        }

        PENDING_DRAW_SCENE = true

        console.info('loadInstances - END')
    }

    /**
    * Textures
    */
    Viewer.prototype.getTexture = function (textureName) {
        if (this._textures[textureName]) {
            return this._textures[textureName]
        }

        return this._defaultTexture
    }

    /**
     * Draw
     */
    Viewer.prototype.drawInstances = function () {
        if (KEEP_NOT_SELECTED_OBJECTS_COLOR || this._selectedObjects.length === 0) {
            this.drawConceptualFaces(true, g_instances, g_geometries)
            this.drawConceptualFaces(false, g_instances, g_geometries)
            this.drawConceptualFacesPolygons(g_instances, g_geometries)
            this.drawLines(g_instances, g_geometries)
            this.drawPoints(g_instances, g_geometries)
            this.drawSelectedInstances()
            this.drawPickedInstance()
        } else {
            this.drawSelectedInstances()
            this.drawPickedInstance()
            this.drawNotSelectedConceptualFaces()
        }

        this.drawGrid()

        this.drawSelectedInstancesLabels()
    }

    Viewer.prototype.drawSceneInstances = function () {
        if (!this._viewCoordinateSystem) {
            return
        }

        this.drawConceptualFaces(true, g_sceneInstances, g_sceneGeometries)
        this.drawConceptualFaces(false, g_sceneInstances, g_sceneGeometries)
        this.drawConceptualFacesPolygons(g_sceneInstances, g_sceneGeometries)
        this.drawLines(g_sceneInstances, g_sceneGeometries)
    }

    Viewer.prototype.drawNavigatorInstances = function () {
        if (!this._viewNavigator) {
            return
        }

        /*
         * Projection matrix
         */
        mat4.identity(this._mtxProjection)
        mat4.perspective(
            45,
            1,
            0.001,
            1000000.0,
            this._mtxProjection
        )

        gl.uniformMatrix4fv(
            this._shaderProgram.ProjectionMatrix,
            false,
            this._mtxProjection
        )

        /*
         * Model-View matrix
         */
        mat4.identity(this._mtxModelView)
        mat4.translate(this._mtxModelView, [0, 0, -5])

        mat4.multiply(this._mtxProjection, this._mtxModelView, this._mtxInversePMV)
        mat4.inverse(this._mtxInversePMV)

        mat4.rotate(this._mtxModelView, (this._rotateX * Math.PI) / 180, [1, 0, 0])
        mat4.rotate(this._mtxModelView, (this._rotateY * Math.PI) / 180, [0, 1, 0])

        gl.uniformMatrix4fv(
            this._shaderProgram.ModelViewMatrix,
            false,
            this._mtxModelView
        )

        /*
         * Normal matrix
         */
        gl.uniformMatrix3fv(this._shaderProgram.NormalMatrix, false, mat4.toMat3(this._mtxModelView))

        gl.viewport(
            (gl.canvas.width / 2.5) - (NAVIGATION_VIEW_LENGTH),
            100,
            NAVIGATION_VIEW_LENGTH,
            NAVIGATION_VIEW_LENGTH)

        this._updateProjectionMatrix = false
        this._applyTranslations = false

        try {
            this.drawConceptualFaces(true, g_navigatorInstances, g_navigatorGeometries)
            this.drawConceptualFaces(false, g_navigatorInstances, g_navigatorGeometries)
            this.drawConceptualFacesPolygons(g_navigatorInstances, g_navigatorGeometries)
            this.drawLines(g_navigatorInstances, g_navigatorGeometries)
        }
        catch (ex) {
            console.error(ex);
        }

        this._updateProjectionMatrix = true
        this._applyTranslations = true
    }

    /**
     * VBO
     */
    Viewer.prototype.setVBO = function (geometry) {
        if (!geometry || !geometry.VBO) {
            return false
        }

        gl.bindBuffer(gl.ARRAY_BUFFER, geometry.VBO)
        gl.vertexAttribPointer(
            this._shaderProgram.VertexPosition,
            3,
            gl.FLOAT,
            false,
            geometry.vertexSizeInBytes,
            0
        )
        gl.enableVertexAttribArray(this._shaderProgram.VertexPosition)

        gl.vertexAttribPointer(
            this._shaderProgram.VertexNormal,
            3,
            gl.FLOAT,
            true,
            geometry.vertexSizeInBytes,
            12
        )
        gl.enableVertexAttribArray(this._shaderProgram.VertexNormal)

        if (geometry.vertexSizeInBytes === 32) {
            gl.vertexAttribPointer(
                this._shaderProgram.UV,
                2,
                gl.FLOAT,
                false,
                geometry.vertexSizeInBytes,
                24
            )
            gl.enableVertexAttribArray(this._shaderProgram.UV)
        }

        return true
    }

    /**
     * Transformation
     */
    Viewer.prototype.applyTransformationMatrix = function (matrix) {
        /*
         * Default matrices
         */
        this.setDefultMatrices()

        if (!!matrix && matrix.length === 16) {
            /*
             * Model-View matrix
             */
            mat4.identity(this._mtxModelView)

            if (this._applyTranslations) {
                mat4.translate(this._mtxModelView, this._eyeVector)
            }
            else {
                mat4.translate(this._mtxModelView, [0, 0, -5])
            }

            mat4.rotate(
                this._mtxModelView,
                (this._rotateX * Math.PI) / 180,
                [1, 0, 0]
            )
            mat4.rotate(
                this._mtxModelView,
                (this._rotateY * Math.PI) / 180,
                [0, 1, 0]
            )

            /*
             * Fit the image
             */
            if (this._applyTranslations) {
                // [0.0 -> X/Y/Zmin + X/Y/Zmax]
                mat4.translate(this._mtxModelView, [
                    -this._worldDimensions.Xmin,
                    -this._worldDimensions.Ymin,
                    -this._worldDimensions.Zmin,
                ])

                // Center
                mat4.translate(this._mtxModelView, [
                    -(this._worldDimensions.Xmax - this._worldDimensions.Xmin) / 2,
                    -(this._worldDimensions.Ymax - this._worldDimensions.Ymin) / 2,
                    -(this._worldDimensions.Zmax - this._worldDimensions.Zmin) / 2,
                ])
            }

            /*
             * Transformation matrix
             */
            var mtxTransformation = mat4.create()
            mtxTransformation[0] = matrix[0]
            mtxTransformation[1] = matrix[1]
            mtxTransformation[2] = matrix[2]
            mtxTransformation[3] = matrix[3]

            mtxTransformation[4] = matrix[4]
            mtxTransformation[5] = matrix[5]
            mtxTransformation[6] = matrix[6]
            mtxTransformation[7] = matrix[7]

            mtxTransformation[8] = matrix[8]
            mtxTransformation[9] = matrix[9]
            mtxTransformation[10] = matrix[10]
            mtxTransformation[11] = matrix[11]

            mtxTransformation[12] = matrix[12]
            mtxTransformation[13] = matrix[13]
            mtxTransformation[14] = matrix[14]
            mtxTransformation[15] = matrix[15]

            mat4.multiply(this._mtxModelView, mtxTransformation, this._mtxModelView)

            gl.uniformMatrix4fv(
                this._shaderProgram.ModelViewMatrix,
                false,
                this._mtxModelView
            )

            /*
            * Normal matrix
            */
            gl.uniformMatrix3fv(this._shaderProgram.NormalMatrix, false, mat4.toMat3(this._mtxModelView))
        } // if (!!matrix && (matrix.length == 16))
    }

    /**
     * Triangles
     */
    Viewer.prototype.drawConceptualFaces = function (opaqueObjects, instances, geometries) {
        if (!this._viewTriangles) {
            return
        }

        if ((instances.length === 0) || (geometries.length === 0)) {
            return
        }

        this.setDefultMatrices()

        gl.uniform1f(this._shaderProgram.EnableLighting, 1.0)
        gl.uniform1f(this._shaderProgram.EnableTexture, 0.0)

        if (!opaqueObjects) {
            gl.enable(gl.BLEND)
            gl.blendEquation(gl.FUNC_ADD)
            gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA)
        }

        try {
            for (let i = 0; i < instances.length; i++) {
                if (!instances[i].visible) {
                    continue
                }

                if (this._pickedObject - 1 === i) {
                    continue
                }

                let index = this._selectedObjects.indexOf(i + 1)
                if (index !== -1) {
                    continue
                }

                for (let g = 0; g < instances[i].geometry.length; g++) {
                    let geometry = geometries[instances[i].geometry[g]]
                    if (!geometry.conceptualFaces) {
                        continue
                    }

                    this.applyTransformationMatrix(instances[i].matrix[g])

                    if (!this.setVBO(geometry)) {
                        console.error('Internal error!')
                        continue
                    }

                    for (let j = 0; j < geometry.conceptualFaces.length; j++) {
                        let conceptualFace = geometry.conceptualFaces[j]
                        if (!conceptualFace.IBO) {
                            continue
                        }

                        if (opaqueObjects) {
                            if (conceptualFace.material.transparency < 1.0) {
                                continue
                            }
                        } else {
                            if (conceptualFace.material.transparency === 1.0) {
                                continue
                            }
                        }

                        if (conceptualFace.material.texture) {
                            gl.uniform1f(this._shaderProgram.EnableTexture, 1.0)

                            gl.activeTexture(gl.TEXTURE0)
                            gl.bindTexture(
                                gl.TEXTURE_2D,
                                this.getTexture(conceptualFace.material.texture.name))

                            gl.uniform1i(this._shaderProgram.Sampler, 0)
                        } // if (conceptualFace.material.texture)
                        else {
                            gl.uniform3f(
                                this._shaderProgram.AmbientMaterial,
                                conceptualFace.material.ambient[0],
                                conceptualFace.material.ambient[1],
                                conceptualFace.material.ambient[2])
                            gl.uniform3f(
                                this._shaderProgram.DiffuseMaterial,
                                conceptualFace.material.diffuse[0],
                                conceptualFace.material.diffuse[1],
                                conceptualFace.material.diffuse[2])
                            gl.uniform3f(
                                this._shaderProgram.SpecularMaterial,
                                conceptualFace.material.specular[0],
                                conceptualFace.material.specular[1],
                                conceptualFace.material.specular[2])
                            // #todo
                            //gl.uniform3f(
                            //  this._shaderProgram.uMaterialEmissiveColor,
                            //  conceptualFace.material.emissive[0] / 3.0,
                            //  conceptualFace.material.emissive[1] / 3.0,
                            //  conceptualFace.material.emissive[2] / 3.0)
                            gl.uniform1f(
                                this._shaderProgram.Transparency,
                                conceptualFace.material.transparency)
                        } // else if (conceptualFace.material.texture)

                        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, conceptualFace.IBO)
                        gl.drawElements(
                            gl.TRIANGLES,
                            conceptualFace.IBO.count,
                            gl.UNSIGNED_SHORT,
                            0)

                        if (conceptualFace.material.texture) {
                            gl.uniform1f(this._shaderProgram.EnableTexture, 0.0)
                        }
                    } // for (let j = ...
                } // for (let g = ...
            } // for (let i = ...
        } catch (ex) {
            console.error(ex)
        }

        if (!opaqueObjects) {
            gl.disable(gl.BLEND)
        }
    }

    /**
     * Triangles
     */
    Viewer.prototype.drawNotSelectedConceptualFaces = function () {
        if (!this._viewTriangles) {
            return
        }

        if (g_instances.length === 0) {
            return
        }

        this.setDefultMatrices()

        gl.uniform1f(this._shaderProgram.EnableLighting, 1.0)
        gl.uniform1f(this._shaderProgram.EnableTexture, 0.0)

        gl.uniform3f(
            this._shaderProgram.AmbientMaterial,
            NOT_SELECTED_OBJECT_COLOR[0],
            NOT_SELECTED_OBJECT_COLOR[1],
            NOT_SELECTED_OBJECT_COLOR[2])
        gl.uniform3f(
            this._shaderProgram.SpecularMaterial,
            NOT_SELECTED_OBJECT_COLOR[0],
            NOT_SELECTED_OBJECT_COLOR[1],
            NOT_SELECTED_OBJECT_COLOR[2])
        gl.uniform3f(
            this._shaderProgram.DiffuseMaterial,
            NOT_SELECTED_OBJECT_COLOR[0],
            NOT_SELECTED_OBJECT_COLOR[1],
            NOT_SELECTED_OBJECT_COLOR[2])
        // #todo
        //gl.uniform3f(
        //  this._shaderProgram.uMaterialEmissiveColor,
        //  NOT_SELECTED_OBJECT_COLOR[0],
        //  NOT_SELECTED_OBJECT_COLOR[1],
        //  NOT_SELECTED_OBJECT_COLOR[2])
        gl.uniform1f(this._shaderProgram.Transparency, NOT_SELECTED_OBJECT_TRANSPARENCY)

        gl.enable(gl.BLEND)
        gl.blendEquation(gl.FUNC_ADD)
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA)

        try {
            for (let i = 0; i < g_instances.length; i++) {
                if (!g_instances[i].visible) {
                    continue
                }

                if (this._pickedObject - 1 === i) {
                    continue
                }

                var index = this._selectedObjects.indexOf(i + 1)
                if (index !== -1) {
                    continue
                }

                for (let g = 0; g < g_instances[i].geometry.length; g++) {
                    let geometry = g_geometries[g_instances[i].geometry[g]]
                    if (!geometry.conceptualFaces) {
                        continue
                    }

                    this.applyTransformationMatrix(g_instances[i].matrix[g])

                    if (!this.setVBO(geometry)) {
                        console.error('Internal error!')
                        continue
                    }

                    for (let j = 0; j < geometry.conceptualFaces.length; j++) {
                        let conceptualFace = geometry.conceptualFaces[j]
                        if (!conceptualFace.IBO) {
                            continue
                        }

                        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, conceptualFace.IBO)
                        gl.drawElements(
                            gl.TRIANGLES,
                            conceptualFace.IBO.count,
                            gl.UNSIGNED_SHORT,
                            0)
                    } // for (let j = ...
                } // for (let g = ...
            } // for (let i = ...
        } catch (ex) {
            console.error(ex)
        }

        gl.disable(gl.BLEND)
    }

    /**
     * Conceptual faces polygons
     */
    Viewer.prototype.drawConceptualFacesPolygons = function (instances, geometries) {
        if (!this._viewWireframes) {
            return
        }

        if ((instances.length === 0) || (geometries.length === 0)) {
            return
        }

        this.setDefultMatrices()

        gl.uniform1f(this._shaderProgram.EnableLighting, 0.0)
        gl.uniform1f(this._shaderProgram.EnableTexture, 0.0)

        gl.uniform3f(this._shaderProgram.AmbientMaterial, 0.0, 0.0, 0.0)
        gl.uniform1f(this._shaderProgram.Transparency, 1.0)
        gl.uniform3f(this._shaderProgram.SpecularMaterial, 0.0, 0.0, 0.0)
        gl.uniform3f(this._shaderProgram.DiffuseMaterial, 0.0, 0.0, 0.0)
        // #todo
        //gl.uniform3f(this._shaderProgram.uMaterialEmissiveColor, 0.0, 0.0, 0.0) 

        try {
            for (let i = 0; i < instances.length; i++) {
                if (!instances[i].visible) {
                    continue
                }

                for (let g = 0; g < instances[i].geometry.length; g++) {
                    let geometry = geometries[instances[i].geometry[g]]
                    if (!geometry.conceptualFacesPolygons) {
                        continue
                    }

                    this.applyTransformationMatrix(instances[i].matrix[g])

                    if (!this.setVBO(geometry)) {
                        console.error('Internal error!')
                        continue
                    }

                    for (let j = 0; j < geometry.conceptualFacesPolygons.length; j++) {
                        let conceptualFacesPolygons = geometry.conceptualFacesPolygons[j]
                        if (!conceptualFacesPolygons.IBO) {
                            continue
                        }

                        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, conceptualFacesPolygons.IBO)
                        gl.drawElements(
                            gl.LINES,
                            conceptualFacesPolygons.IBO.count,
                            gl.UNSIGNED_SHORT,
                            0)
                    } // for (let j = ...
                } // for (let g = ...
            } // for (let i = ...
        } catch (ex) {
            console.error(ex)
        }
    }

    /**
     * Lines
     */
    Viewer.prototype.drawLines = function (instances, geometries) {
        if (!this._viewLines) {
            return
        }

        if ((instances.length === 0) || (geometries.length === 0)) {
            return
        }

        this.setDefultMatrices()

        gl.uniform1f(this._shaderProgram.EnableLighting, 0.0)
        gl.uniform1f(this._shaderProgram.EnableTexture, 0.0)

        gl.uniform3f(this._shaderProgram.AmbientMaterial, 0.0, 0.0, 0.0)
        gl.uniform1f(this._shaderProgram.Transparency, 1.0)
        gl.uniform3f(this._shaderProgram.SpecularMaterial, 0.0, 0.0, 0.0)
        gl.uniform3f(this._shaderProgram.DiffuseMaterial, 0.0, 0.0, 0.0)
        // #todo
        //gl.uniform3f(this._shaderProgram.uMaterialEmissiveColor, 0.0, 0.0, 0.0)

        try {
            for (let i = 0; i < instances.length; i++) {
                if (!instances[i].visible) {
                    continue
                }

                for (let g = 0; g < instances[i].geometry.length; g++) {
                    let geometry = geometries[instances[i].geometry[g]]
                    if (!geometry.conceptualFaces) {
                        continue
                    }

                    this.applyTransformationMatrix(instances[i].matrix[g])

                    if (!this.setVBO(geometry)) {
                        console.error('Internal error!')
                        continue
                    }

                    for (let j = 0; j < geometry.conceptualFaces.length; j++) {
                        let conceptualFace = geometry.conceptualFaces[j]
                        if (!conceptualFace.IBOLines) {
                            continue
                        }

                        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, conceptualFace.IBOLines)
                        gl.drawElements(
                            gl.LINES,
                            conceptualFace.IBOLines.count,
                            gl.UNSIGNED_SHORT,
                            0)
                    } // for (let j = ...
                } // for (let g = ...
            } // for (let i = ...
        } catch (ex) {
            console.error(ex)
        }
    }

    /**
     * Points
     */
    Viewer.prototype.drawPoints = function (instances, geometries) {
        if (!this._viewPoints) {
            return
        }

        if ((instances.length === 0) || (geometries.length === 0)) {
            return
        }

        this.setDefultMatrices()

        gl.uniform1f(this._shaderProgram.EnableLighting, 0.0)
        gl.uniform1f(this._shaderProgram.EnableTexture, 0.0)

        gl.uniform3f(this._shaderProgram.AmbientMaterial, 0.0, 0.0, 0.0)
        gl.uniform1f(this._shaderProgram.Transparency, 1.0)
        gl.uniform3f(this._shaderProgram.SpecularMaterial, 0.0, 0.0, 0.0)
        gl.uniform3f(this._shaderProgram.DiffuseMaterial, 0.0, 0.0, 0.0)
        // #todo
        //gl.uniform3f(this._shaderProgram.uMaterialEmissiveColor, 0.0, 0.0, 0.0)

        try {
            for (let i = 0; i < instances.length; i++) {
                if (!instances[i].visible) {
                    continue
                }

                for (let g = 0; g < instances[i].geometry.length; g++) {
                    let geometry = geometries[instances[i].geometry[g]]
                    if (!geometry.conceptualFaces) {
                        continue
                    }

                    this.applyTransformationMatrix(instances[i].matrix[g])

                    if (!this.setVBO(geometry)) {
                        console.error('Internal error!')
                        continue
                    }

                    for (let j = 0; j < geometry.conceptualFaces.length; j++) {
                        var conceptualFace = geometry.conceptualFaces[j]
                        if (!conceptualFace.IBOPoints) {
                            continue
                        }

                        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, conceptualFace.IBOPoints)
                        gl.drawElements(
                            gl.POINTS,
                            conceptualFace.IBOPoints.count,
                            gl.UNSIGNED_SHORT,
                            0)
                    } // for (let j = ...
                } // for (let g = ...
            } // for (let i = ...
        } catch (ex) {
            console.error(ex)
        }
    }

    /**
     * selection support
     */
    Viewer.prototype.drawInstancesSelectionFrameBuffer = function () {
        if (g_instances.length === 0) {
            return
        }

        try {
            /*
             * Encoding the selection colors
             */
            if (this._instancesSelectionColors.length === 0) {
                var step = 1.0 / 255.0

                for (let i = 0; i < g_instances.length; i++) {
                    // build selection color
                    var R = Math.floor((i + 1) / (255 * 255))
                    if (R >= 1.0) {
                        R *= step
                    }

                    var G = Math.floor((i + 1) / 255)
                    if (G >= 1.0) {
                        G *= step
                    }

                    var B = Math.floor((i + 1) % 255)
                    B *= step

                    this._instancesSelectionColors.push([R, G, B])
                } // for (let i = ...
            } // if (this._instancesSelectionColors.length == 0)

            this.setDefultMatrices()

            gl.uniform1f(this._shaderProgram.EnableLighting, 0.0)
            gl.uniform1f(this._shaderProgram.EnableTexture, 0.0)
            gl.uniform1f(this._shaderProgram.Transparency, 1.0)

            gl.bindFramebuffer(gl.FRAMEBUFFER, this._selectionFramebuffer)

            gl.viewport(
                0,
                0,
                this._selectionFramebuffer.width,
                this._selectionFramebuffer.height)
            gl.clearColor(0.0, 0.0, 0.0, 0.0)
            gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT)

            for (let i = 0; i < g_instances.length; i++) {
                if (!g_instances[i].visible) {
                    continue
                }

                gl.uniform3f(
                    this._shaderProgram.AmbientMaterial,
                    this._instancesSelectionColors[i][0],
                    this._instancesSelectionColors[i][1],
                    this._instancesSelectionColors[i][2])

                for (let g = 0; g < g_instances[i].geometry.length; g++) {
                    let geometry = g_geometries[g_instances[i].geometry[g]]
                    if (!geometry.conceptualFaces) {
                        continue
                    }

                    this.applyTransformationMatrix(g_instances[i].matrix[g])

                    if (!this.setVBO(geometry)) {
                        console.error('Internal error!')
                        continue
                    }

                    for (let j = 0; j < geometry.conceptualFaces.length; j++) {
                        let conceptualFaces = geometry.conceptualFaces[j]
                        if (!conceptualFaces.IBO) {
                            continue
                        }

                        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, conceptualFaces.IBO)
                        gl.drawElements(
                            gl.TRIANGLES,
                            conceptualFaces.IBO.count,
                            gl.UNSIGNED_SHORT,
                            0)
                    } // for (let j = ...
                } // for (let i = ...
            } // for (let g = ...

            gl.bindFramebuffer(gl.FRAMEBUFFER, null)
        } catch (ex) {
            console.error(ex)
        }
    }

    /**
     * Selection support
     */
    Viewer.prototype.drawSelectedInstances = function () {
        if (!this._viewTriangles) {
            return
        }

        if (g_instances.length === 0) {
            return
        }

        if (this._selectedObjects.length === 0) {
            return
        }

        this.setDefultMatrices()

        gl.uniform1f(this._shaderProgram.EnableLighting, 1.0)
        gl.uniform1f(this._shaderProgram.EnableTexture, 0.0)

        gl.enable(gl.BLEND)
        gl.blendEquation(gl.FUNC_ADD)
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA)

        try {
            gl.uniform3f(
                this._shaderProgram.AmbientMaterial,
                SELECTED_OBJECT_AMBIENT_COLOR[0],
                SELECTED_OBJECT_AMBIENT_COLOR[1],
                SELECTED_OBJECT_AMBIENT_COLOR[2])
            gl.uniform3f(
                this._shaderProgram.SpecularMaterial,
                SELECTED_OBJECT_SPECULAR_COLOR[0],
                SELECTED_OBJECT_SPECULAR_COLOR[1],
                SELECTED_OBJECT_SPECULAR_COLOR[2])
            gl.uniform3f(
                this._shaderProgram.DiffuseMaterial,
                SELECTED_OBJECT_DIFFUSE_COLOR[0],
                SELECTED_OBJECT_DIFFUSE_COLOR[1],
                SELECTED_OBJECT_DIFFUSE_COLOR[2])
            // #todo
            //gl.uniform3f(
            //  this._shaderProgram.uMaterialEmissiveColor,
            //  SELECTED_OBJECT_EMISSIVE_COLOR[0],
            //  SELECTED_OBJECT_EMISSIVE_COLOR[1],
            //  SELECTED_OBJECT_EMISSIVE_COLOR[2])
            gl.uniform1f(this._shaderProgram.Transparency, SELECTED_OBJECT_TRANSPARENCY)

            for (let index = 0; index < this._selectedObjects.length; index++) {
                let instance = g_instances[this._selectedObjects[index] - 1]

                for (let g = 0; g < instance.geometry.length; g++) {
                    let geometry = g_geometries[instance.geometry[g]]
                    if (!geometry.conceptualFaces) {
                        continue
                    }

                    this.applyTransformationMatrix(instance.matrix[g])

                    if (!this.setVBO(geometry)) {
                        console.error('Internal error!')
                        continue
                    }

                    for (let j = 0; j < geometry.conceptualFaces.length; j++) {
                        let conceptualFace = geometry.conceptualFaces[j]
                        if (!conceptualFace.IBO) {
                            continue
                        }

                        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, conceptualFace.IBO)
                        gl.drawElements(
                            gl.TRIANGLES,
                            conceptualFace.IBO.count,
                            gl.UNSIGNED_SHORT,
                            0)
                    } // for (let j = ...
                } // for (let g = ...
            } // for (let index = ...
        } catch (ex) {
            console.error(ex)
        }

        gl.disable(gl.BLEND)
    }

    /**
     * Grid
     */
    Viewer.prototype.drawGrid = function () {
        if (!this._viewGrid) {
            return
        }

        this.setDefultMatrices()

        gl.uniform1f(this._shaderProgram.EnableLighting, 1.0)
        gl.uniform1f(this._shaderProgram.EnableTexture, 0.0)

        gl.uniform3f(this._shaderProgram.AmbientMaterial, 0.0, 0.0, 0.0)
        gl.uniform3f(this._shaderProgram.SpecularMaterial, 0.0, 0.0, 0.0)
        gl.uniform3f(this._shaderProgram.DiffuseMaterial, 0.0, 0.0, 0.0)
        // #todo
        //gl.uniform3f(this._shaderProgram.uMaterialEmissiveColor, 0.0, 0.0, 0.0)
        gl.uniform1f(this._shaderProgram.Transparency, 1.0)

        try {
            if (this._gridVBO === null) {
                const step = (2.0 / (this._gridLinesCount + 1))
                let gridVertices = []

                let line = 0;
                for (let x = -1.0 + step; (x < 1.0) && (line < this._gridLinesCount); x += step, line++) {
                    gridVertices.push(x, this._worldDimensions.Ymin, -1.0)
                    gridVertices.push(x, this._worldDimensions.Ymin, 1.0)
                }

                line = 0;
                for (let z = -1.0 + step; (z < 1.0) && (line < this._gridLinesCount); z += step, line++) {
                    gridVertices.push(-1.0, this._worldDimensions.Ymin, z)
                    gridVertices.push(1.0, this._worldDimensions.Ymin, z)
                }

                this._gridVBO = gl.createBuffer()

                gl.bindBuffer(gl.ARRAY_BUFFER, this._gridVBO)
                gl.bufferData(
                    gl.ARRAY_BUFFER,
                    new Float32Array(gridVertices),
                    gl.STATIC_DRAW)

                this._gridVBO.count = gridVertices.length / 3
            } // if (this._gridVBO === null)

            gl.bindBuffer(gl.ARRAY_BUFFER, this._gridVBO)
            gl.vertexAttribPointer(
                this._shaderProgram.VertexPosition,
                3,
                gl.FLOAT,
                false,
                12,
                0)

            gl.enableVertexAttribArray(this._shaderProgram.VertexPosition)

            gl.drawArrays(gl.LINES, 0, this._gridVBO.count)
        } catch (ex) {
            console.error(ex)
        }
    }

    /**
     * Labels
     */
    Viewer.prototype.drawSelectedInstancesLabels = function () {
        if (this._divHeight !== null) {
            this._divHeight.style.display = 'none'
        }

        if (this._divWidth !== null) {
            this._divWidth.style.display = 'none'
        }

        if (this._divDepth !== null) {
            this._divDepth.style.display = 'none'
        }

        if (!this._viewBBox && !this._viewBBoxX && !this._viewBBoxY && !this._viewBBoxZ) {
            return
        }

        if (g_instances.length === 0) {
            return
        }

        if (this._selectedObjects.length === 0) {
            return
        }

        this.setDefultMatrices()

        gl.uniform1f(this._shaderProgram.EnableLighting, 1.0)
        gl.uniform1f(this._shaderProgram.EnableTexture, 0.0)

        gl.uniform3f(this._shaderProgram.AmbientMaterial, 0.0, 0.0, 0.0)
        gl.uniform3f(this._shaderProgram.SpecularMaterial, 0.0, 0.0, 0.0)
        gl.uniform3f(this._shaderProgram.DiffuseMaterial, 0.0, 0.0, 0.0)
        // #todo
        //gl.uniform3f(this._shaderProgram.uMaterialEmissiveColor, 0.0, 0.0, 0.0)
        gl.uniform1f(this._shaderProgram.Transparency, 1.0)

        try {
            for (let index = 0; index < this._selectedObjects.length; index++) {
                var instance = g_instances[this._selectedObjects[index] - 1]

                if (!instance.visible) {
                    continue
                }

                if (instance.BBVBO === undefined) {
                    instance.BBVBO = gl.createBuffer();

                    gl.bindBuffer(gl.ARRAY_BUFFER, instance.BBVBO);

                    gl.bufferData(
                        gl.ARRAY_BUFFER,
                        new Float32Array([
                            // front
                            // front left
                            instance.Xmin, instance.Ymin, instance.Zmin,
                            instance.Xmin, instance.Ymax, instance.Zmin,
                            // front right
                            instance.Xmax, instance.Ymin, instance.Zmin,
                            instance.Xmax, instance.Ymax, instance.Zmin,
                            // front top
                            instance.Xmin, instance.Ymax, instance.Zmin,
                            instance.Xmax, instance.Ymax, instance.Zmin,
                            // front bottom
                            instance.Xmin, instance.Ymin, instance.Zmin,
                            instance.Xmax, instance.Ymin, instance.Zmin,
                            // back
                            // back left
                            instance.Xmin, instance.Ymin, instance.Zmax,
                            instance.Xmin, instance.Ymax, instance.Zmax,
                            // back right
                            instance.Xmax, instance.Ymin, instance.Zmax,
                            instance.Xmax, instance.Ymax, instance.Zmax,
                            // back top
                            instance.Xmin, instance.Ymax, instance.Zmax,
                            instance.Xmax, instance.Ymax, instance.Zmax,
                            // back bottom
                            instance.Xmin, instance.Ymin, instance.Zmax,
                            instance.Xmax, instance.Ymin, instance.Zmax,
                            // left
                            // left top
                            instance.Xmin, instance.Ymax, instance.Zmin,
                            instance.Xmin, instance.Ymax, instance.Zmax,
                            // left bottom
                            instance.Xmin, instance.Ymin, instance.Zmin,
                            instance.Xmin, instance.Ymin, instance.Zmax,
                            // right
                            // right top
                            instance.Xmax, instance.Ymax, instance.Zmin,
                            instance.Xmax, instance.Ymax, instance.Zmax,
                            // right bottom
                            instance.Xmax, instance.Ymin, instance.Zmin,
                            instance.Xmax, instance.Ymin, instance.Zmax,
                        ]),
                        gl.STATIC_DRAW);
                } // if (instance.BBVBO === undefined)

                const divContainerElement = document.querySelector("#labels-container");

                if (this._viewBBoxX) {
                    if (this._divHeight === null) {
                        this._divHeight = document.createElement("div")
                        this._divHeight.className = "floating-div tooltip-div"

                        this._txtHeight = document.createTextNode("")
                        this._divHeight.appendChild(this._txtHeight)

                        divContainerElement.appendChild(this._divHeight)
                    }

                    this._divHeight.style.display = 'block'

                    // height - front left
                    this.drawLengthLabel(this._divHeight, this._txtHeight,
                        instance.Xmin, instance.Ymin, instance.Zmin,
                        instance.Xmin, instance.Ymax, instance.Zmin,
                        'h = ')
                }

                if (this._viewBBoxY) {
                    if (this._divWidth === null) {
                        this._divWidth = document.createElement("div")
                        this._divWidth.className = "floating-div tooltip-div"

                        this._txtWidth = document.createTextNode("")
                        this._divWidth.appendChild(this._txtWidth)

                        divContainerElement.appendChild(this._divWidth)
                    }

                    this._divWidth.style.display = 'block'

                    // width - front bottom
                    this.drawLengthLabel(this._divWidth, this._txtWidth,
                        instance.Xmin, instance.Ymin, instance.Zmin,
                        instance.Xmax, instance.Ymin, instance.Zmin,
                        'w = ')
                }

                if (this._viewBBoxZ) {
                    if (this._divDepth === null) {
                        this._divDepth = document.createElement("div")
                        this._divDepth.className = "floating-div tooltip-div"

                        this._txtDepth = document.createTextNode("")
                        this._divDepth.appendChild(this._txtDepth)

                        divContainerElement.appendChild(this._divDepth)
                    }

                    this._divDepth.style.display = 'block'

                    // depth - right bottom
                    this.drawLengthLabel(this._divDepth, this._txtDepth,
                        instance.Xmax, instance.Ymin, instance.Zmin,
                        instance.Xmax, instance.Ymin, instance.Zmax,
                        'd = ')
                }

                /*
                 * BBO
                 */
                if (this._viewBBox) {
                    gl.bindBuffer(gl.ARRAY_BUFFER, instance.BBVBO)

                    gl.vertexAttribPointer(
                        this._shaderProgram.VertexPosition,
                        3,
                        gl.FLOAT,
                        false,
                        12,
                        0
                    )

                    gl.enableVertexAttribArray(this._shaderProgram.VertexPosition)

                    gl.drawArrays(gl.LINES, 0, 24);
                }
            } // for (let i = ...
        } catch (ex) {
            console.error(ex)
        }
    }

    /**
     * Labels
     */
    Viewer.prototype.drawLengthLabel = function (divLabel, txtLabel,
        x1, y1, z1,
        x2, y2, z2,
        prefix) {
        var length = Math.sqrt(
            Math.pow(x2 - x1, 2.0) +
            Math.pow(y2 - y1, 2.0) +
            Math.pow(z2 - z1, 2.0))

        var point3D = [
            (x1 + x2) / 2.0,
            (y1 + y2) / 2.0,
            (z1 + z2) / 2.0,
            1.0,
        ]

        var pointScreen = [0, 0, 0, 0]

        // I
        var mtxModelViewProjection = mat4.create()
        mat4.multiply(this._mtxProjection, this._mtxModelView, mtxModelViewProjection)
        mat4.multiplyVec4(mtxModelViewProjection, point3D, pointScreen)

        // II
        //mat4.multiplyVec4(this._mtxModelView, point3D, pointScreen)
        //mat4.multiplyVec4(this._mtxProjection, pointScreen, pointScreen)

        pointScreen[0] /= pointScreen[3]
        pointScreen[1] /= pointScreen[3]

        divLabel.style.left = Math.floor(((pointScreen[0] + 1.0) / 2.0) * gl.canvas.width) + "px"
        divLabel.style.top = Math.floor(((1.0 - pointScreen[1]) / 2.0) * gl.canvas.height) + "px"
        txtLabel.nodeValue = prefix + (length * this._scaleFactor).toFixed(4)
    }

    /**
     * Selection support
     */
    Viewer.prototype.drawPickedInstance = function () {
        if (!this._viewTriangles) {
            return
        }

        if (g_instances.length === 0) {
            return
        }

        if (this._pickedObject === -1) {
            return
        }

        if (this._selectedObjects.indexOf(this._pickedObject) !== -1) {
            // it is selected
            return
        }

        this.setDefultMatrices()

        gl.uniform1f(this._shaderProgram.EnableLighting, 1.0)
        gl.uniform1f(this._shaderProgram.EnableTexture, 0.0)

        gl.enable(gl.BLEND)
        gl.blendEquation(gl.FUNC_ADD)
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA)

        try {
            gl.uniform3f(
                this._shaderProgram.AmbientMaterial,
                PICKED_OBJECT_AMBIENT_COLOR[0],
                PICKED_OBJECT_AMBIENT_COLOR[1],
                PICKED_OBJECT_AMBIENT_COLOR[2])
            gl.uniform3f(
                this._shaderProgram.SpecularMaterial,
                PICKED_OBJECT_SPECULAR_COLOR[0],
                PICKED_OBJECT_SPECULAR_COLOR[1],
                PICKED_OBJECT_SPECULAR_COLOR[2])
            gl.uniform3f(
                this._shaderProgram.DiffuseMaterial,
                PICKED_OBJECT_DIFFUSE_COLOR[0],
                PICKED_OBJECT_DIFFUSE_COLOR[1],
                PICKED_OBJECT_DIFFUSE_COLOR[2])
            // #todo
            //gl.uniform3f(
            //  this._shaderProgram.uMaterialEmissiveColor,
            //  PICKED_OBJECT_EMISSIVE_COLOR[0],
            //  PICKED_OBJECT_EMISSIVE_COLOR[1],
            //  PICKED_OBJECT_EMISSIVE_COLOR[2])
            gl.uniform1f(this._shaderProgram.Transparency, PICKED_OBJECT_TRANSPARENCY)

            var instance = g_instances[this._pickedObject - 1]
            for (let g = 0; g < instance.geometry.length; g++) {
                let geometry = g_geometries[instance.geometry[g]]

                this.applyTransformationMatrix(instance.matrix[g])

                if (!this.setVBO(geometry)) {
                    console.error('Internal error!')
                    continue
                }

                for (let j = 0; j < geometry.conceptualFaces.length; j++) {
                    let conceptualFace = geometry.conceptualFaces[j]
                    if (!conceptualFace.IBO) {
                        continue
                    }

                    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, conceptualFace.IBO)
                    gl.drawElements(
                        gl.TRIANGLES,
                        conceptualFace.IBO.count,
                        gl.UNSIGNED_SHORT,
                        0)
                } // for (let j = ...
            } // for (let g = ...
        } catch (ex) {
            console.error(ex)
        }

        gl.disable(gl.BLEND)
    }
}

/**
 * Viewer
 */
var g_viewer = new Viewer()

/**
 * Render
 */
function renderLoop() {
    utils.requestAnimFrame(renderLoop)

    if (!PENDING_DRAW_SCENE) {
        return
    }

    g_viewer.drawScene()

    if (typeof onViewerRender === typeof Function) {
        onViewerRender()
    }

    if (PENDING_DRAW_SCENE_COUNT > 0) {
        PENDING_DRAW_SCENE_COUNT = PENDING_DRAW_SCENE_COUNT - 1
        PENDING_DRAW_SCENE = PENDING_DRAW_SCENE_COUNT > 0 ? true : false
    }
    else {
        PENDING_DRAW_SCENE = false
    }
}

/**
 * Event handler
 */
$(window).resize(function () {
    PENDING_DRAW_SCENE = true

    if (typeof onWindowResize === typeof Function) {
        onWindowResize()
    }
})

