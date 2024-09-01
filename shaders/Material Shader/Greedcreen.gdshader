shader_type spatial;
render_mode unshaded;

uniform vec2 grid_size = vec2(5.0, 5.0);
uniform vec4 active_color : source_color = vec4(1.0, 0.5, 0.0, 1.0); // Orange
uniform vec4 inactive_color : source_color = vec4(0.5, 0.5, 0.5, 1.0); // Grey
uniform vec4 line_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float line_thickness : hint_range(0.0, 0.1) = 0.02;
uniform sampler2D state_texture;

void fragment() {
    vec2 grid_uv = UV * grid_size;
    vec2 cell = floor(grid_uv);
    vec2 cell_uv = fract(grid_uv);
    
    // Check cell state
    vec2 state_uv = (cell + vec2(0.5)) / grid_size;
    float state = texture(state_texture, state_uv).r;
    
    // Determine if we're on a grid line
    float line_x = step(1.0 - line_thickness, cell_uv.x) + step(cell_uv.x, line_thickness);
    float line_y = step(1.0 - line_thickness, cell_uv.y) + step(cell_uv.y, line_thickness);
    float is_line = max(line_x, line_y);
    
    // Mix colors based on state and grid lines
    vec4 cell_color = mix(inactive_color, active_color, state);
    vec4 color = mix(cell_color, line_color, is_line);
    
    ALBEDO = color.rgb;
}