shader_type spatial;

uniform vec3 albedo : source_color = vec3(0.1);
uniform vec3 emission_color : source_color = vec3(1.0, 0.0, 0.0);
uniform float emission_strength : hint_range(0.0, 10.0) = 5.0;
uniform float roughness : hint_range(0.0, 1.0) = 0.2;
uniform float metallic : hint_range(0.0, 1.0) = 0.0;
uniform float fresnel : hint_range(0.0, 1.0) = 0.1;
uniform float rim_thickness : hint_range(0.0, 1.0) = 0.1;

void fragment() {
    ALBEDO = albedo;
    EMISSION = emission_color * emission_strength;
    METALLIC = metallic;
    ROUGHNESS = roughness;
    
    float fresnel_effect = pow(1.0 - dot(NORMAL, VIEW), 5.0) * fresnel;
    float rim = 1.0 - dot(NORMAL, VIEW);
    rim = smoothstep(1.0 - rim_thickness, 1.0, rim);
    
    EMISSION += (fresnel_effect + rim) * emission_color * emission_strength;
}