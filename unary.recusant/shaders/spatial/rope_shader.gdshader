shader_type spatial;
render_mode cull_front;

uniform sampler2D albedo_texture : source_color, filter_linear_mipmap, repeat_enable;
uniform vec4 albedo_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float roughness : hint_range(0.0, 1.0) = 0.90;
uniform float metallic : hint_range(0.0, 1.0) = 0.00;

uniform vec3 global_wind_dir = vec3(1.0, 0.0, 0.0);
uniform float global_wind_speed : hint_range(0.0, 10.0) = 0.0;
uniform float global_wind_strength : hint_range(0.0, 10.0) = 0.0;
uniform float global_wind_amplitude : hint_range(0.0, 5.0) = 0.0;
uniform float global_wind_frequency : hint_range(0.0, 5.0) = 0.0;

vec2 unpack_two_floats(float packed, float maxLimit) 
{
    return unpackUnorm2x16(floatBitsToUint(packed)) * maxLimit;
}

// Wind source pos (Vector3) + Wind source strength (float) / Wind source radius (float)
instance uniform vec4 unique_data1 = vec4(0.0, 0.0, 0.0, 0.0);
// Rope length (float)
instance uniform vec4 unique_data2 = vec4(0.0, 0.0, 0.0, 0.0);

float _hash(float n) 
{
    return fract(sin(n) * 43758.5453123);
}

float _noise1(float x) 
{
    float i = floor(x);
    float f = fract(x);
    float u = f * f * (3.0 - 2.0 * f);
    return mix(_hash(i), _hash(i + 1.0), u);
}

float _fbm(float x) 
{
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < 4; i++) 
	{
        v += a * _noise1(x);
        x *= 2.0;
        a *= 0.5;
    }
    return v;
}

float remap(float value, float from1, float to1, float from2, float to2)
{
	return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
}

void vertex() 
{
	vec3 point_wind_origin = unique_data1.rgb;
	vec2 wind_data = unpack_two_floats(unique_data1.a, 50.0);
	
	float point_wind_strength = wind_data.x;
	float point_wind_radius = wind_data.y;
	
	float rope_length = unique_data2.r;
	
	float strength = global_wind_strength * remap(rope_length, 0.0, 20.0, 0.0001, 1.0);
	float amplitude = global_wind_amplitude * remap(rope_length, 0.0, 20.0, 0.0001, 1.0);
	
    float t = UV.y;
    vec3 world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float rope_weight = sin(t * PI);
    float phase = t * TAU + TIME * global_wind_speed;
    vec3 gDir = normalize(global_wind_dir);
    vec3 up = vec3(0.0, 1.0, 0.0);
	
	float newStrength = max(0.0, strength + amplitude * sin(TIME * global_wind_frequency * TAU));
	
    float swayPrimary = sin(phase) * newStrength * rope_weight;

    vec3 perp = normalize(cross(gDir, up));
	
    float swayPerp = sin(phase * 3.37) * newStrength * 0.25 * rope_weight;
    float swayVert = sin(phase * 0.61) * newStrength * 0.10 * rope_weight;

    vec3 globalDisplace = gDir * swayPrimary + perp * swayPerp + up * swayVert;
	
	// Point calculations
	vec3 pointDisplace = vec3(0.0);
	
	vec3 delta = world_pos - point_wind_origin;
	float dist = length(delta);
	float influence = 1.0 - smoothstep(0.0, point_wind_radius, dist);

	// Radial blast direction
	vec3 blastDir = normalize(delta + vec3(1e-5));
	blastDir.y *= 0.3; // flatten vertical component

	// Simulates rotating blades: rectified sine → always outward pulses
	float bladePhase = TIME * global_wind_speed * 3.0 - dist * 0.8;
	float bladePulse = abs(sin(bladePhase));

	pointDisplace = blastDir * bladePulse * influence * point_wind_strength * rope_weight;

	// Rotational swirl around the point source axis
	float swirl = sin(TIME * global_wind_speed * 2.0 + t * 4.0) * 0.3;
	vec3 swirlV = cross(blastDir, up) * swirl * influence * point_wind_strength * rope_weight;
	pointDisplace += swirlV;
	
    vec3 world_displace = globalDisplace + pointDisplace;
    vec3 local_displace = (inverse(MODEL_MATRIX) * vec4(world_displace, 0.0)).xyz;
    VERTEX += local_displace;
}

void fragment() 
{
    ALBEDO = texture(albedo_texture, UV).rgb * albedo_color.rgb;
    ROUGHNESS = roughness;
    METALLIC = metallic;
}
