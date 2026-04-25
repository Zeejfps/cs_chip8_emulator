#version 330 core

const vec2 positions[4] = vec2[4](
    vec2(-1.0, -1.0),
    vec2( 1.0, -1.0),
    vec2(-1.0,  1.0),
    vec2( 1.0,  1.0)
);

const vec2 uvs[4] = vec2[4](
    vec2(0.0, 1.0),
    vec2(1.0, 1.0),
    vec2(0.0, 0.0),
    vec2(1.0, 0.0)
);

out vec2 vUv;

void main() {
    gl_Position = vec4(positions[gl_VertexID], 0.0, 1.0);
    vUv = uvs[gl_VertexID];
}
