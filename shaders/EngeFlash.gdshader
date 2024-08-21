shader_type canvas_item;

uniform vec4 glow_color : source_color = vec4(0.0, 0.5, 1.0, 1.0);
uniform float glow_width : hint_range(0.0, 0.5) = 0.1;
uniform float glow_intensity : hint_range(0.0, 1.0) = 0.5;

void fragment() {
    vec2 uv = UV;

    // Calculate distance from edges
    float dist_x = min(uv.x, 1.0 - uv.x);
    float dist_y = min(uv.y, 1.0 - uv.y);
    float edge_dist = min(dist_x, dist_y);

    // Create smooth edge glow
    float glow = smoothstep(0.0, glow_width, edge_dist);
    glow = 1.0 - glow;
    glow = pow(glow, 2.0); // Sharpen the glow

    // Apply color and intensity
    vec4 final_color = glow_color;
    final_color.a *= glow * glow_intensity;

    COLOR = final_color;
}