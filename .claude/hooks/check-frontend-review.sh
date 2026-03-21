#!/bin/bash
# Fires on Stop. If web/ files were modified in this session, injects a mandatory
# reminder that Claude must run frontend-vue-reviewer before the session is done.
if { git diff --name-only origin/main...HEAD -- web/ 2>/dev/null
     git diff --name-only HEAD -- web/ 2>/dev/null
     git diff --name-only --cached -- web/ 2>/dev/null
   } | grep -q .; then
  cat <<'EOF'
{"continue":false,"stopReason":"MANDATORY: Frontend files under web/ were modified in this session. You MUST invoke the frontend-vue-reviewer agent now before this session is considered done. This is a non-negotiable quality gate — do not skip it."}
EOF
fi
