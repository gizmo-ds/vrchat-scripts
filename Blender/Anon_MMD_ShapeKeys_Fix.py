import bpy

dictionary = {
    "eyebrow_serious": "真面目",
    "eyebrow_trouble": "困る",
    "eyebrow_anger": "怒り",
    "eyebrow_up": "上",
    "eyebrow_down": "下",
    "eye_sad": "悲しい",
    "eye_close": "まばたき",
    "eye_smile": "笑い",
    "eye_smile.L": "ウィンク",
    "eye_smile.R": "ウィンク右",
    "eye_close.L": "ウィンク２",
    "eye_close.R": "ｳｨﾝｸ２右",
    "eye_> <": "はぅ",
    "eye_nagomi": "なごみ",
    "eye_open": "びっくり",
    "eye_jito": "じと目",
    "mouth_a": "あ",
    "vrc.v_ch": "い",
    "mouth_u": "う",
    "mouth_e": "え",
    "vrc.v_oh": "お",
    "mouth_△": "▲",
    "mouth_^": "∧",
    "mouth_ω": "ω",
    "mouth_wa1": "ω□",
    "mouth_awawa": "えー",
    "mouth_smile": "にやり",
    "mouth_u": "う",
    "mouth_tongue_out": "ぺろっ",
    "cheek2": "頬染め",
    "eye_shirome": "白目",
    "eye_big": "瞳大",
    "eye_small": "瞳小",
    "eye_pleasure": "睨む",
}

selected_object = bpy.context.object

selected_object.shape_key_add(name="=====MMD=====")
for sk in selected_object.data.shape_keys.key_blocks:
    if sk.name in dictionary:
        sk.value = 1
        selected_object.shape_key_add(name=dictionary.get(sk.name), from_mix=True)
        sk.value = 0
