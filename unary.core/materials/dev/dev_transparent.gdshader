shader_type spatial;
render_mode unshaded;

uniform sampler2D albedo : source_color;
const float alpha = 0.25;

void fragment() 
{
	ALBEDO = texture(albedo, UV).rgb;
	ALPHA = alpha;
}
