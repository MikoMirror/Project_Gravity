shader_type canvas_item;

uniform vec4 dot_color : source_color = vec4(1.0, 1.0, 1.0, 0.8);
uniform float dot_size : hint_range(0.0, 1.0) = 0.003;
uniform float border_size : hint_range(0.0, 1.0) = 0.001;
uniform vec4 border_color : source_color = vec4(0.0, 0.0, 0.0, 0.8);

void fragment() {
    vec2 center = vec2(0.5, 0.5);
    float dist = distance(UV, center);

    if (dist < dot_size - border_size) {
        COLOR = dot_color;
    } else if (dist < dot_size) {
        COLOR = border_color;
    } else {
        COLOR = vec4(0.0);
    }
}
