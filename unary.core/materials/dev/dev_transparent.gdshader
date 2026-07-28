shader_type spatial;
render_mode unshaded;

uniform sampler2D albedo : source_color, repeat_enable;
uniform float alpha : hint_range(0.0, 1.0) = 0.5;

// The texture's own base color, divided back out of the sample before tinting. Feeding it the shade the
// texture is actually drawn in cancels that hue out and leaves a white/black image, so `color` is then free
// to tint it to anything instead of multiplying into it - a red tint over a green texture would otherwise
// come out muddy brown and near invisible. White is the identity and leaves the sample untouched.
instance uniform vec3 bleach : source_color = vec3(1.0, 1.0, 1.0);

instance uniform vec3 color : source_color = vec3(1.0, 1.0, 1.0);

varying vec3 local_position;
varying vec3 local_normal;

void vertex()
{
	local_position = VERTEX;
	local_normal = NORMAL;
}

void fragment()
{
	vec3 weights = pow(abs(local_normal), vec3(8.0)) + vec3(1e-6);
	weights /= (weights.x + weights.y + weights.z);
	vec2 uv_x = vec2(-local_position.z * sign(local_normal.x), -local_position.y);
	vec2 uv_y = vec2(local_position.x * sign(local_normal.y), local_position.z);
	vec2 uv_z = vec2(local_position.x * sign(local_normal.z), -local_position.y);
	vec3 tex_x = texture(albedo, uv_x).rgb;
	vec3 tex_y = texture(albedo, uv_y).rgb;
	vec3 tex_z = texture(albedo, uv_z).rgb;
	vec3 result = tex_x * weights.x + tex_y * weights.y + tex_z * weights.z;
	// Guarded against a zero channel, which carries no detail to recover and would otherwise blow up.
	result = clamp(result / max(bleach, vec3(0.001)), 0.0, 1.0);
	result *= color;
	ALBEDO = result;
	ALPHA = alpha;
}
