export default `
precision highp float;

uniform vec3 uPointLightingLocation;
uniform float uMaterialShininess;
uniform vec3 uMaterialAmbientColor;
uniform float uTransparency;
uniform vec3 uMaterialDiffuseColor;
uniform vec3 uMaterialSpecularColor;
uniform vec3 uMaterialEmissiveColor;
uniform sampler2D uSampler;

varying float vSelectionMode;
varying float vUseTexture;
varying vec4 vPosition;
varying vec3 vTransformedNormal;
varying vec2 vTextureCoord;

void main(void) {
    if (vSelectionMode == 0.0) {
        if (vUseTexture == 0.0) {
            vec3 ambientLightWeighting = vec3(0.001, 0.001, 0.001);

            vec3 lightDirection = normalize(uPointLightingLocation - vPosition.xyz);
            vec3 normal = normalize(vTransformedNormal);

            vec3 eyeDirection = normalize(-vPosition.xyz);
            vec3 reflectionDirection = reflect(-lightDirection, normal);

            float specularLightBrightness = pow(max(dot(reflectionDirection, eyeDirection), 0.0), uMaterialShininess);
            vec3 specularLightWeighting = vec3(0.8, 0.8, 0.8) * specularLightBrightness;

            float diffuseLightBrightness = max(dot(normal, lightDirection), 0.0);
            vec3 diffuseLightWeighting = vec3(0.8, 0.8, 0.8) * diffuseLightBrightness;

            //default: 0.75, 0.125, 2.2
            float contrast = 1.50;
            float brightness = 0.175;
            float gamma = 1.25;

            vec4 color = vec4(
                uMaterialAmbientColor * ambientLightWeighting +
                uMaterialDiffuseColor * diffuseLightWeighting +
                uMaterialSpecularColor * specularLightWeighting +
                uMaterialEmissiveColor,
                uTransparency);

            vec4 newColor = vec4(0.0, 0.0, 0.0, uTransparency);
            newColor.r = (pow(color.r, gamma) - 0.5) * contrast + brightness + 0.5;
            newColor.g = (pow(color.g, gamma) - 0.5) * contrast + brightness + 0.5;
            newColor.b = (pow(color.b, gamma) - 0.5) * contrast + brightness + 0.5;
            gl_FragColor = newColor;
        } // if (vUseTexture == ...
        else {
            gl_FragColor = texture2D(uSampler, vTextureCoord);
        }
    } // if (vSelectionMode == ...
    else {
        gl_FragColor = vec4(uMaterialAmbientColor, uTransparency);
    }
}
`;