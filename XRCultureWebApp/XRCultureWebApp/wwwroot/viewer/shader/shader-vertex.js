export default `
uniform mat4 uMVMatrix;
uniform mat4 uPMatrix;
uniform mat4 uNMatrix;

uniform float uUseTexture;
uniform float uSelectionMode;

attribute vec3 aVertexPosition;
attribute vec3 aVertexNormal;
attribute vec2 aTextureCoord;

varying float vSelectionMode;
varying float vUseTexture;
varying vec4 vPosition;
varying vec3 vTransformedNormal;
varying vec2 vTextureCoord;

void main(void) {
    vec4 vertex = uMVMatrix * vec4(aVertexPosition, 1.0);
    vPosition = vertex;

    gl_Position = uPMatrix * vertex;

    if (uSelectionMode == 0.0) {
        if (uUseTexture == 0.0) {
            vTransformedNormal = vec3(uNMatrix * vec4(aVertexNormal, 1.0));
        } else {
            vTextureCoord = aTextureCoord;
        }

        vUseTexture = uUseTexture;
    }

    vSelectionMode = uSelectionMode;
}
`;