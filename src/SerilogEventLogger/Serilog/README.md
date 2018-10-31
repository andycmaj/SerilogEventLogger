this path is pretty haxxin.
serilog has really good object -> IEnumerable<propertyName:propertyValue> conversion,
but it's all private.

I stole it to use it here. It enables anonymous object -> mutable property list conversion to allow us to flatten event data for serilog events.
