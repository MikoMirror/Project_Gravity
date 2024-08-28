shader_type canvas_item;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

// Anti fish eye (negative amount) / fish eye (positive)
uniform float effect_amount : hint_range(-2.5, 2.5) = 1.0;
// Blur amount
uniform float blur_amount : hint_range(0.0, 5.0) = 0.0;

vec4 blur(sampler2D tex, vec2 uv, vec2 pixel_size) {
    vec4 color = texture(tex, uv);
    vec2 ps = pixel_size * blur_amount;

    color += texture(tex, uv + vec2(-ps.x, -ps.y));
    color += texture(tex, uv + vec2(-ps.x, 0.0));
    color += texture(tex, uv + vec2(-ps.x, ps.y));
    color += texture(tex, uv + vec2(0.0, -ps.y));
    color += texture(tex, uv + vec2(0.0, ps.y));
    color += texture(tex, uv + vec2(ps.x, -ps.y));
    color += texture(tex, uv + vec2(ps.x, 0.0));
    color += texture(tex, uv + vec2(ps.x, ps.y));

    return color / 9.0;
}

void fragment() {
    vec2 iResolution = 1.0 / SCREEN_PIXEL_SIZE;
    vec4 fragCoord = FRAGCOORD;

    //normalized coords
    vec2 p = fragCoord.xy / iResolution.x;

    //screen proportion
    float prop = iResolution.x / iResolution.y;

    //center coords
    vec2 m = vec2(0.5, 0.5 / prop);

    //vector from center to current fragment
    vec2 d = p - m;

    // distance of pixel from center
    float r = sqrt(dot(d, d));

    float power = effect_amount;

    //radius of 1:1 effect
    float bind;

    //stick to borders
    if (power > 0.0)
        bind = sqrt(dot(m, m));
    else {
        if (prop < 1.0)
            bind = m.x;
        else
            bind = m.y;
    }

    vec2 uv;
    //fisheye
    if (power > 0.0)
        uv = m + normalize(d) * tan(r * power) * bind / tan( bind * power);
    //antifisheye
    else if (power < 0.0)
        uv = m + normalize(d) * atan(r * -power * 10.0) * bind / atan(-power * bind * 10.0);
    //no effect for power = 1.0
    else
        uv = p;
    uv.y *= prop;

    // Apply blur
    vec4 col = blur(SCREEN_TEXTURE, uv, SCREEN_PIXEL_SIZE);

    COLOR = col;
}