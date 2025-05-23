var Module = {
    onRuntimeInitialized: function () {
        console.log('onRuntimeInitialized')
    },
}

var g_fileName = null;
var g_logCache = [];

function jsLogCallback(event) {
    g_logCache.push(event + '\n');
}

function printLogCache() {
    let txtLog = document.getElementById('txtLog');
    for (let i = 0; i < g_logCache.length; i++) {
        txtLog.value += g_logCache[i];
    }

    g_logCache = [];

    txtLog.scrollTop = txtLog.scrollHeight;
}

function embeddedMode() {
    try {
        return window.self !== window.top
    } catch (e) {
        return true
    }
}

function getFileExtension(file) {
    if (file && file.length > 4) {
        return file.split('.').pop();
    }

    return null
}

function addContent(fileName, fileExtension, fileContent) {
    console.log('addContent BEGIN: ' + fileName)

    // Cache
    let instances = [...g_instances]
    let geometries = [...g_geometries]

    // Load new instances
    g_instances = []
    g_geometries = []

    Module.unload()
    Module['FS_createDataFile']('/data/', 'input.ifc', fileContent, true, true)

    if (fileExtension === 'dxf') {
        Module.loadDXF(true, !embeddedMode())
    }
    else if ((fileExtension === 'bin') || (fileExtension == 'binz')) {
        Module.loadBIN(true, !embeddedMode(), SCALE_AND_CENTER)
    }
    else if ((fileExtension == 'dae') || (fileExtension == 'zae')) {
        Module.loadDAE(true, !embeddedMode())
    }
    else if (fileExtension == 'obj') {
        Module.loadOBJ(true, !embeddedMode())
    }
    else if ((fileExtension == 'gml') ||
        (fileExtension == 'citygml') ||
        (fileExtension == 'xml') ||
        (fileExtension == 'json')) {
        Module.loadGIS(fileName, true, !embeddedMode(), SCALE_AND_CENTER)
    }
    else {
        Module.loadSTEP(true, !embeddedMode(), false) // SCALE_AND_CENTER; Patch for Multiple IFC Model - World Coordinates
    }

    FS.unlink('/data/' + 'input.ifc')

    loadInstances(false)

    // Re-index Geometry-s/Add Instance-s
    for (let i = 0; i < g_instances.length; i++) {
        for (let g = 0; g < g_instances[i].geometry.length; g++) {
            g_instances[i].geometry[g] = g_instances[i].geometry[g] + geometries.length
        }

        instances.push(g_instances[i])
    }

    // Add Geometry-s
    for (let g = 0; g < g_geometries.length; g++) {
        geometries.push(g_geometries[g])
    }

    // Update
    g_instances = [...instances]
    g_geometries = [...geometries]

    // Update Viewer
    g_viewer._scaleFactor = SCALE_AND_CENTER ? Module.getScale() : 1.0
    g_viewer.loadInstances()

    console.log('addContent END: ' + fileName)
}

function loadContent(fileName, fileExtension, fileContent) {
    // WebGL Cleanup
    g_viewer.deleteBuffers()

    // Data Cleanup
    g_instances = []
    g_geometries = []

    addContent(fileName, fileExtension, fileContent)
}

function loadZAE(fileName, data) {
    var jsZip = new JSZip()
    jsZip.loadAsync(data).then(function (zip) {
        let daeFile = getDAEFile(zip)
        if (daeFile) {
            zip.file(daeFile).async('string').then(function (fileContent) {
                loadContent(fileName, 'dae', fileContent)

                var textureCnt = Module.getTextureCnt()
                for (let t = 0; t < textureCnt; t++) {
                    var textureName = Module.getTextureInfo(t + 1)
                    loadTexture(zip, textureName)
                }
            })
        }
    })
}

function loadBINZ(fileName, data) {
    var jsZip = new JSZip()
    jsZip.loadAsync(data).then(function (zip) {
        let binFile = getBINFile(zip)
        if (binFile) {
            zip.file(binFile).async('Uint8Array').then(function (fileContent) {
                loadContent(fileName, 'bin', fileContent)

                var textureCnt = Module.getTextureCnt()
                for (let t = 0; t < textureCnt; t++) {
                    var textureName = Module.getTextureInfo(t + 1)
                    loadTexture(zip, textureName)
                }
            })
        }
    })
}

function loadFile(file) {
    resetFields()

    var fileReader = new FileReader()
    fileReader.onload = function () {
        var fileContent = new Uint8Array(fileReader.result)

        var fileExtension = getFileExtension(file.name)
        if (fileExtension === 'zae') {
            try {
                loadZAE(file.name, fileContent)
            }
            catch (e) {
                console.error(e)
            }
        }
        else if (fileExtension === 'binz') {
            try {
                loadBINZ(file.name, fileContent)
            }
            catch (e) {
                console.error(e)
            }
        }
        else {
            loadContent(file.name, fileExtension, fileContent)
        }
    }

    fileReader.readAsArrayBuffer(file)
}

function loadInstances(updateViewer) {
    try {
        Module.createCache()

        let texturesCount = Module.getTextureCnt()
        console.log('Textures Count: ' + texturesCount)

        var geometryCnt = Module.getGeometryCnt()
        console.log('Geometries Count: ' + geometryCnt)

        for (let g = 0; g < geometryCnt; g++) {
            let geometry = {
                id: Module.getIndexGeometryItem(g),
                vertices: [],
                conceptualFaces: [],
                conceptualFacesPolygons: [],
                vertexSizeInBytes: texturesCount > 0 ? 32 : 24,
            }

            // Vertices
            let vertices = texturesCount > 0 ?
                Module.getGeometryItemVerticesWithTextureCoordinates(geometry.id) :
                Module.getGeometryItemVertices(geometry.id)
            let vertexCnt = vertices.size()
            for (let v = 0; v < vertexCnt; v++) {
                geometry.vertices.push(vertices.get(v))
            }

            // Faces
            var faceCnt = Module.getFaceCnt(geometry.id)
            for (let group = 0; group < faceCnt; group++) {
                let material = Module.getFaceMaterial(geometry.id, group)
                let textureIndex = Module.getFaceTexture(geometry.id, group)
                let conceptualFace = {
                    material: {
                        ambient: [material.get(0), material.get(1), material.get(2)],
                        diffuse: [material.get(3), material.get(4), material.get(5)],
                        specular: [material.get(6), material.get(7), material.get(8)],
                        emissive: [material.get(9), material.get(10), material.get(10)],
                        transparency: material.get(12),
                    },
                    indicesTriangles: [],
                    indicesLines: [],
                    indicesPoints: [],
                }

                if (textureIndex >= 0) {
                    if (textureIndex >= texturesCount) {
                        textureIndex = texturesCount - 1; // bug in WASM
                    }
                    conceptualFace.material.texture = {}
                    conceptualFace.material.texture.name = Module.getTextureInfo(textureIndex + 1)
                }

                let indices = Module.getFaceTriangleIndices(geometry.id, group)
                let indicesSize = indices.size()
                for (let i = 0; i < indicesSize; i++) {
                    conceptualFace.indicesTriangles.push(indices.get(i))
                }

                indices = Module.getFaceEdgeIndices(geometry.id, group)
                indicesSize = indices.size()
                for (let i = 0; i < indicesSize; i++) {
                    conceptualFace.indicesLines.push(indices.get(i))
                }

                indices = Module.getFacePointIndices(geometry.id, group)
                indicesSize = indices.size()
                for (let i = 0; i < indicesSize; i++) {
                    conceptualFace.indicesPoints.push(indices.get(i))
                }

                geometry.conceptualFaces.push(conceptualFace)
            } // for (let group = ...

            // Wireframes
            var wireframeCnt = Module.getWireframeCnt(geometry.id)
            for (let group = 0; group < wireframeCnt; group++) {
                let wireframes = {
                    indices: [],
                }

                let indicesWF = Module.getWireframeIndices(geometry.id, group)
                let mySizeWF = indicesWF.size()
                for (var i = 0; i < mySizeWF; i++) {
                    wireframes.indices.push(indicesWF.get(i))
                }

                geometry.conceptualFacesPolygons.push(wireframes)
            } // for (let group = ...

            g_geometries.push(geometry)
        } // for (let g = ...

        var instanceCnt = Module.getInstanceCnt()
        console.log('Instances Count: ' + instanceCnt)

        for (let i = 0; i < instanceCnt; i++) {
            var bbox = Module.getInstanceBBox(i)

            var instance = {
                uri: Module.getInstanceUri(i),
                guid: Module.getInstanceGuid(i),
                label: Module.getInstanceLabel(i),
                visible: true,
                Xmin: bbox.get(0),
                Ymin: bbox.get(1),
                Zmin: bbox.get(2),
                Xmax: bbox.get(3),
                Ymax: bbox.get(4),
                Zmax: bbox.get(5),
                geometry: [],
                matrix: [],
            }

            let instanceGeometryCnt = Module.getInstanceGeometryCnt(i)
            for (let r = 0; r < instanceGeometryCnt; r++) {
                let instanceGeometryRef = Module.getInstanceGeometryRef(i, r)
                instance.geometry.push(instanceGeometryRef)

                let matrix = []
                let instanceGeometryMatrix = Module.getInstanceGeometryMatrix(i, 0)
                if (!!instanceGeometryMatrix && instanceGeometryMatrix.size() === 16) {
                    for (let i = 0; i < instanceGeometryMatrix.size(); i++) {
                        matrix.push(instanceGeometryMatrix.get(i))
                    }
                }
                instance.matrix.push(matrix)
            } // for (let r = ...

            g_instances.push(instance)
        } // for (let i = ...

        if (updateViewer) {
            g_viewer._scaleFactor = SCALE_AND_CENTER ? Module.getScale() : 1.0
            g_viewer.loadInstances()
        }
    }
    catch (ex) {
        console.error(ex)
    }
}

function loadSceneInstances() {
    Module.loadCoordinateSystem()

    loadInstances(true)

    for (let i = 0; i < g_instances.length; i++) {
        g_sceneInstances.push(g_instances[i])
    }

    for (let g = 0; g < g_geometries.length; g++) {
        g_sceneGeometries.push(g_geometries[g])
    }

    g_instances = []
    g_geometries = []
}

function loadNavigatorInstances() {
    Module.loadNavigator()

    loadInstances(true)

    for (let i = 0; i < g_instances.length; i++) {
        g_navigatorInstances.push(g_instances[i])
    }

    for (let g = 0; g < g_geometries.length; g++) {
        g_navigatorGeometries.push(g_geometries[g])
    }

    g_instances = []
    g_geometries = []
}

function clearFields() { }

// Emscripten/Docker
function readFileFileSystem(file, callback) {
    var rawFile = new XMLHttpRequest();
    rawFile.open("GET", file);
    rawFile.setRequestHeader("Content-Type", "text/xml");
    rawFile.setRequestHeader("X-Requested-With", "XMLHttpRequest");
    rawFile.setRequestHeader("Access-Control-Allow-Origin", "*");
    rawFile.onreadystatechange = function () {
        if (rawFile.readyState === 4 && rawFile.status === 200) {
            callback(rawFile.responseText);
        }
    }
    rawFile.send();
}

// Emscripten/Docker
function loadFileByPath(file) {
    resetFields()

    readFileFileSystem(`${file}`, function (fileContent) {
        try {
            var fileExtension = getFileExtension(file)
            if (fileExtension === 'zae') {
                try {
                    loadZAE(file.name, fileContent)
                }
                catch (e) {
                    console.error(e)
                }
            }
            else {
                loadContent(file.name, fileExtension, fileContent)
            }
        }
        catch (e) {
            console.error(e);
        }
    });
}

function readFileByUri(file, callback) {
    try {
        var rawFile = new XMLHttpRequest()
        rawFile.open('GET', "http://localhost:8088/fileservice/byUri?fileUri=" + encodeURIComponent(file))
        rawFile.setRequestHeader("Content-type", "application/json; charset=utf-8")
        rawFile.onreadystatechange = function () {
            if (rawFile.readyState === 4 && rawFile.status === 200) {
                callback(rawFile.responseText)
            }
        }
        rawFile.send()
    }
    catch (ex) {
        console.error(ex)
    }
}

function getDAEFile(zip) {
    if (zip) {
        for (let [fileName] of Object.entries(zip.files)) {
            if (getFileExtension(fileName) === 'dae') {
                return fileName
            }
        }
    }
    return null
}

function getBINFile(zip) {
    if (zip) {
        for (let [fileName] of Object.entries(zip.files)) {
            if (getFileExtension(fileName) === 'bin') {
                return fileName
            }
        }
    }
    return null
}

function loadTexture(zip, textureName) {
    if (zip) {
        zip.file(textureName).async('blob').then(function (blob) {
            g_viewer._textures[textureName] = g_viewer.createTextureBLOB(blob)
        })
    }
}

function loadFileByUri(file) {
    let fileExtension = getFileExtension(file)

    if (fileExtension === 'zae') {
        try {
            JSZipUtils.getBinaryContent(file, function (err, data) {
                if (err) {
                    throw err
                }
                loadZAE(file, data)
            })
        }
        catch (e) {
            console.error(e)
        }
    }
    else if (fileExtension === 'binz') {
        try {
            // Use fetch API to get the binary data
            fetch('/DownloadFolder?handler=File&file=' + encodeURIComponent(file))
                .then(response => {
                    if (!response.ok) throw new Error('Network response was not ok');
                    return response.arrayBuffer();
                })
                .then(data => {
                    loadBINZ(file, new Uint8Array(data));
            })
                .catch(e => {
                    console.error(e);
                });
        }
        catch (e) {
            console.error(e);
        }
    } else {
        readFileByUri(`${file}`, function (fileContent) {
            try {
                loadContent(file, fileExtension, fileContent)
            }
            catch (e) {
                console.error(e)
            }
        })
    }
}
