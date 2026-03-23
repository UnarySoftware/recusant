@tool
class_name Trigger extends VMFEntityNode

var brush_entity = preload("res://unary.core/sources/ecs/BrushEntity.cs")

func _entity_setup(_entity: VMFEntity):
	$Area3D/CollisionShape3D.shape = get_entity_shape();
	
	var debug_color = _entity.data.get("rendercolor")
	
	$Area3D/CollisionShape3D.debug_fill = true
	
	if debug_color is Color:
		$Area3D/CollisionShape3D.debug_color = debug_color
	elif debug_color is Vector3:
		$Area3D/CollisionShape3D.debug_color = Color8(debug_color.x, debug_color.y, debug_color.z)
	
	var group = _entity.data.get("groupname") as String
	$Area3D.add_to_group.call_deferred(group, true)

	self.set_script(brush_entity)
	call_deferred("_ready")
