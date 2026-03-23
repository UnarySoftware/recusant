@tool
class_name TextEditor extends VMFEntityNode

var target_script = preload("res://unary.core/sources/ecs/EditorOnly.cs")

func _entity_setup(_entity: VMFEntity):
	transform = get_entity_transform(_entity)
	rotate_y(deg_to_rad(-90))
	$Label3D.text = str(_entity.data.get("message"))
	
	var font_size : int = _entity.data.get("textsize") * 4
	
	$Label3D.font_size = font_size
	$Label3D.offset.y = font_size / 2
	$Label3D.offset.x = font_size / 4
	
	var color = _entity.data.get("color")
	
	if color is Color:
		$Label3D.modulate = color
	elif color is Vector3:
		$Label3D.modulate = Color8(color.x, color.y, color.z)
	
	$Label3D.outline_size = font_size / 5
	self.set_script(target_script)
