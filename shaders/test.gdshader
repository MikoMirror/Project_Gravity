shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
uniform float aberration : hint_range(0.0, 1.0) = 0.005;
uniform float brightness : hint_range(0.0, 2.0) = 1.0;
uniform float grain_amount : hint_range(0.0, 0.5) = 0.05;
uniform float grain_size : hint_range(1.0, 10.0) = 1.0;
uniform float square_size : hint_range(0.0, 1.0) = 0.8;
uniform float corner_radius : hint_range(0.0, 0.5) = 0.1;
uniform float border_blur : hint_range(0.0, 0.1) = 0.05;

float random(vec2 uv) {
    return fract(sin(dot(uv, vec2(12.9898, 78.233))) * 43758.5453123);
}

float rounded_square(vec2 uv, float size, float radius) {
    vec2 q = abs(uv) - size + radius;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius;
}

void fragment() {
    vec2 uv = SCREEN_UV;
    vec2 centered_uv = (uv - 0.5) * 2.0;
    
    // Adjust for aspect ratio to make it square
    float aspect = SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x;
    centered_uv.x *= aspect;
    
    // Rounded square vignette
    float square_vignette = rounded_square(centered_uv, square_size, corner_radius);
    square_vignette = smoothstep(-border_blur, border_blur, square_vignette);
    
    // Chromatic aberration
    float offset = aberration * square_vignette;
    vec2 uv_r = uv + vec2(offset, 0.0);
    vec2 uv_b = uv - vec2(offset, 0.0);
    
    vec3 col;
    col.r = texture(SCREEN_TEXTURE, uv_r).r;
    col.g = texture(SCREEN_TEXTURE, uv).g;
    col.b = texture(SCREEN_TEXTURE, uv_b).b;
    
    // Brightness adjustment
    col *= brightness;
    
    // Film grain
    float grain = random(uv * grain_size) * grain_amount;
    col += grain;
    
    // Slight blur (approximation)
    vec2 blur_uv = uv * 0.98 + 0.01;
    vec3 blur_col = texture(SCREEN_TEXTURE, blur_uv).rgb;
    col = mix(col, blur_col, 0.1);
    
    // Color grading for vintage look
    col = mix(col, vec3(dot(col, vec3(0.299, 0.587, 0.114))), 0.2);
    col = mix(col, vec3(0.8, 0.7, 0.6) * dot(col, vec3(0.299, 0.587, 0.114)), 0.1);
    
    // Apply square vignette with blurred border
    col = mix(vec3(0.0), col, 1.0 - square_vignette);
    
    COLOR = vec4(col, 1.0);
}