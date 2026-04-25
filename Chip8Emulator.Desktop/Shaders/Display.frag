#version 330 core

uniform usampler2D uDisplay;
uniform vec4 uPalette[4];

in vec2 vUv;
out vec4 fragColor;

void main() {
    uint v = texture(uDisplay, vUv).r;
    fragColor = uPalette[v & 3u];
}
